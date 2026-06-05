import { Hono } from 'hono'
import { eq, and } from 'drizzle-orm'
import { db, schema } from '../db/index.js'
import { success, created, badRequest, now } from '../utils/response.js'
import { generateImage } from '../services/image-generation.js'
import { saveUploadedFile } from '../utils/storage.js'
import { logTaskError, logTaskStart, logTaskSuccess } from '../utils/task-logger.js'
import { styleScene, aspectRatioToSize, getDramaVisualSettings } from '../utils/style-mapping.js'

const app = new Hono()

// POST /scenes
app.post('/', async (c) => {
  const body = await c.req.json()
  const ts = now()
  const res = db.insert(schema.scenes).values({
    dramaId: body.drama_id,
    episodeId: body.episode_id,
    location: body.location,
    time: body.time || '',
    prompt: body.prompt || body.location,
    createdAt: ts,
    updatedAt: ts,
  }).run()
  const sceneId = Number(res.lastInsertRowid)

  // Link to episode if episode_id provided
  if (body.episode_id) {
    const epId = Number(body.episode_id)
    const existing = db.select().from(schema.episodeScenes)
      .where(and(
        eq(schema.episodeScenes.episodeId, epId),
        eq(schema.episodeScenes.sceneId, sceneId)
      )).all()
    if (!existing.length) {
      db.insert(schema.episodeScenes).values({
        episodeId: epId,
        sceneId: sceneId,
        createdAt: ts,
      }).run()
    }
  }

  const [result] = db.select().from(schema.scenes)
    .where(eq(schema.scenes.id, sceneId)).all()
  return created(c, result)
})

// PUT /scenes/:id
app.put('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  const body = await c.req.json()
  const updates: Record<string, any> = { updatedAt: now() }
  if (body.location !== undefined) updates.location = body.location
  if (body.time !== undefined) updates.time = body.time
  if (body.prompt !== undefined) updates.prompt = body.prompt
  db.update(schema.scenes).set(updates).where(eq(schema.scenes.id, id)).run()
  return success(c)
})

// POST /scenes/:id/generate-image
app.post('/:id/generate-image', async (c) => {
  const id = Number(c.req.param('id'))
  const body = await c.req.json()
  const [scene] = db.select().from(schema.scenes).where(eq(schema.scenes.id, id)).all()
  if (!scene) return badRequest(c, 'Scene not found')
  if (!body.episode_id) return badRequest(c, 'episode_id is required')
  const [ep] = db.select().from(schema.episodes).where(eq(schema.episodes.id, Number(body.episode_id))).all()
  if (!ep) return badRequest(c, 'Episode not found')

  // Load drama visual settings (style + aspect ratio in one query)
  const { style: dramaStyle, aspectRatio } = getDramaVisualSettings(scene.dramaId)
  const styleTag = styleScene(dramaStyle)
  const size = aspectRatioToSize(aspectRatio)

  const prompt = body.prompt || scene.prompt || `${scene.location}, ${scene.time || ''}, ${styleTag}, atmospheric lighting, high quality, no text, no watermark`
  try {
    logTaskStart('SceneImage', 'generate', { sceneId: id, episodeId: ep.id, dramaId: scene.dramaId, location: scene.location, style: dramaStyle, aspectRatio })
    db.update(schema.scenes).set({ status: 'processing', updatedAt: now() }).where(eq(schema.scenes.id, id)).run()
    const genId = await generateImage({ sceneId: id, dramaId: scene.dramaId, prompt, size, configId: ep.imageConfigId ?? undefined })
    logTaskSuccess('SceneImage', 'generate', { sceneId: id, generationId: genId })
    return success(c, { image_generation_id: genId })
  } catch (err: any) {
    logTaskError('SceneImage', 'generate', { sceneId: id, error: err.message })
    db.update(schema.scenes).set({ status: 'failed', updatedAt: now() }).where(eq(schema.scenes.id, id)).run()
    return badRequest(c, err.message)
  }
})

// POST /scenes/:id/upload-image — 上传场景图片
app.post('/:id/upload-image', async (c) => {
  const id = Number(c.req.param('id'))
  const [scene] = db.select().from(schema.scenes).where(eq(schema.scenes.id, id)).all()
  if (!scene) return badRequest(c, 'Scene not found')

  const body = await c.req.parseBody()
  const file = body['file']
  if (!file || !(file instanceof File)) return badRequest(c, 'file is required')

  const buffer = await file.arrayBuffer()
  const localPath = await saveUploadedFile(buffer, 'images', file.name)
  db.update(schema.scenes)
    .set({ imageUrl: localPath, updatedAt: now() })
    .where(eq(schema.scenes.id, id))
    .run()
  logTaskSuccess('SceneImage', 'upload', { sceneId: id, path: localPath })
  return success(c, { image_url: localPath, url: `/${localPath}` })
})

// DELETE /scenes/:id
app.delete('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  // Check storyboard bindings before delete
  const bindings = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.sceneId, id)).all()
  if (bindings.length > 0) {
    return badRequest(c, `场景已被 ${bindings.length} 个分镜引用，无法删除`)
  }
  db.delete(schema.scenes).where(eq(schema.scenes.id, id)).run()
  return success(c)
})

// GET /scenes/:id/storyboard-bindings — 检查场景是否被分镜引用
app.get('/:id/storyboard-bindings', async (c) => {
  const id = Number(c.req.param('id'))
  const bindings = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.sceneId, id)).all()
  return success(c, { bound: bindings.length > 0, storyboard_count: bindings.length })
})

export default app
