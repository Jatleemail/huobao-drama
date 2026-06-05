import { Hono } from 'hono'
import { eq, and } from 'drizzle-orm'
import { db, schema } from '../db/index.js'
import { success, created, badRequest, now } from '../utils/response.js'
import { generateImage } from '../services/image-generation.js'
import { saveUploadedFile } from '../utils/storage.js'
import { logTaskError, logTaskStart, logTaskSuccess } from '../utils/task-logger.js'
import { getDramaVisualSettings, aspectRatioToSize } from '../utils/style-mapping.js'

const app = new Hono()

// GET /api/v1/props?dramaId=1
app.get('/', async (c) => {
  const dramaId = Number(c.req.query('dramaId'))
  if (!dramaId) return badRequest(c, 'dramaId query parameter is required')
  const list = db.select().from(schema.props)
    .where(eq(schema.props.dramaId, dramaId))
    .all()
    .filter(p => !p.deletedAt)
  return success(c, list)
})

// POST /api/v1/props — Create a new prop
app.post('/', async (c) => {
  const body = await c.req.json()
  if (!body.drama_id) return badRequest(c, 'drama_id is required')
  if (!body.name) return badRequest(c, 'name is required')
  const ts = now()
  const res = db.insert(schema.props).values({
    dramaId: body.drama_id,
    name: body.name,
    type: body.type || null,
    description: body.description || null,
    prompt: body.prompt || null,
    imageUrl: body.image_url || null,
    createdAt: ts,
    updatedAt: ts,
  }).run()
  const propId = Number(res.lastInsertRowid)
  const [result] = db.select().from(schema.props).where(eq(schema.props.id, propId)).all()
  return created(c, result)
})

// PUT /api/v1/props/:id
app.put('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  const body = await c.req.json()
  const updates: Record<string, any> = { updatedAt: now() }
  const fieldMap: Record<string, string> = {
    name: 'name', type: 'type', category: 'type',
    description: 'description', prompt: 'prompt',
    imageUrl: 'imageUrl', image_url: 'imageUrl',
  }
  for (const [key, dbKey] of Object.entries(fieldMap)) {
    if (key in body && body[key] !== undefined) {
      updates[dbKey] = body[key]
    }
  }
  db.update(schema.props).set(updates).where(eq(schema.props.id, id)).run()
  return success(c)
})

// DELETE /api/v1/props/:id — 软删除，检查分镜绑定
app.delete('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  // Check if prop is bound to any storyboard
  const bindings = db.select().from(schema.storyboardProps)
    .where(eq(schema.storyboardProps.propId, id)).all()
  if (bindings.length > 0) {
    return c.json({ success: false, error: '该道具已绑定到分镜，无法删除' }, 409)
  }
  db.update(schema.props).set({ deletedAt: now() }).where(eq(schema.props.id, id)).run()
  return success(c)
})

// GET /api/v1/props/:id/storyboard-bindings — 查询道具绑定的分镜
app.get('/:id/storyboard-bindings', async (c) => {
  const id = Number(c.req.param('id'))
  const bindings = db.select().from(schema.storyboardProps)
    .where(eq(schema.storyboardProps.propId, id)).all()
  return success(c, bindings)
})

// POST /api/v1/props/:id/generate-image — 生成道具图片
app.post('/:id/generate-image', async (c) => {
  const id = Number(c.req.param('id'))
  const body = await c.req.json().catch(() => ({}))
  const [prop] = db.select().from(schema.props).where(eq(schema.props.id, id)).all()
  if (!prop) return badRequest(c, 'Prop not found')

  const { style: dramaStyle, aspectRatio } = getDramaVisualSettings(prop.dramaId)
  const size = aspectRatioToSize(aspectRatio)

  const prompt = body.prompt || prop.prompt ||
    `A clean product-shot of ${prop.name}, ${prop.description || ''}, isolated on neutral background, studio lighting, high quality, detailed, no people, no hands, no text, no watermark.`

  try {
    logTaskStart('PropImage', 'generate', { propId: id, dramaId: prop.dramaId, style: dramaStyle, aspectRatio })
    const genId = await generateImage({ propId: id, dramaId: prop.dramaId, prompt, size, configId: body.image_config_id })
    logTaskSuccess('PropImage', 'generate', { propId: id, generationId: genId })
    return success(c, { image_generation_id: genId })
  } catch (err: any) {
    logTaskError('PropImage', 'generate', { propId: id, error: err.message })
    return badRequest(c, err.message)
  }
})

// POST /api/v1/props/:id/upload-image — 上传道具图片
app.post('/:id/upload-image', async (c) => {
  const id = Number(c.req.param('id'))
  const [prop] = db.select().from(schema.props).where(eq(schema.props.id, id)).all()
  if (!prop) return badRequest(c, 'Prop not found')

  const formBody = await c.req.parseBody()
  const file = formBody['file']
  if (!file || !(file instanceof File)) return badRequest(c, 'file is required')

  const buffer = await file.arrayBuffer()
  const localPath = await saveUploadedFile(buffer, 'images', file.name)
  db.update(schema.props)
    .set({ imageUrl: localPath, updatedAt: now() })
    .where(eq(schema.props.id, id))
    .run()
  logTaskSuccess('PropImage', 'upload', { propId: id, path: localPath })
  return success(c, { image_url: localPath, url: `/${localPath}` })
})

export default app
