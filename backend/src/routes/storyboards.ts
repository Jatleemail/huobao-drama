import { Hono } from 'hono'
import { eq } from 'drizzle-orm'
import { db, schema } from '../db/index.js'
import { success, created, now, badRequest } from '../utils/response.js'
import { toSnakeCase } from '../utils/transform.js'
import { generateTTS } from '../services/tts-generation.js'
import { logTaskError, logTaskPayload, logTaskProgress, logTaskStart, logTaskSuccess } from '../utils/task-logger.js'

const app = new Hono()

const IGNORE_TTS_SPEAKERS = /^(环境音|环境声|音效|效果音|sfx|sound ?effect|bgm|背景音|背景音乐|ambient)$/i
const IGNORE_TTS_TEXT = /^(无|无对白|无台词|无旁白|无需配音|无需对白|none|null|n\/a|na|环境音|环境声|音效|效果音|纯音效|纯环境音|只有环境音|仅环境音|背景音|背景音乐|bgm|sfx|ambient)$/i

function parseDialogueForTTS(dialogue?: string | null) {
  const raw = dialogue?.trim() || ''
  if (!raw) return { speaker: '', pureText: '', ignorable: true }
  const speakerMatch = raw.match(/^(.+?)[:：]/)
  const speaker = speakerMatch ? speakerMatch[1].replace(/[（(].+?[)）]/g, '').trim() : ''
  const pureText = raw.replace(/^.+?[:：]\s*/, '').replace(/[（(].+?[)）]/g, '').trim()
  const ignorable = (!!speaker && IGNORE_TTS_SPEAKERS.test(speaker)) || !pureText || IGNORE_TTS_TEXT.test(pureText)
  return { speaker, pureText, ignorable }
}

function syncStoryboardCharacters(storyboardId: number, characterIds: number[]) {
  db.delete(schema.storyboardCharacters)
    .where(eq(schema.storyboardCharacters.storyboardId, storyboardId))
    .run()

  const uniqueIds = [...new Set((characterIds || []).filter(Boolean))]
  if (!uniqueIds.length) return

  for (const characterId of uniqueIds) {
    db.insert(schema.storyboardCharacters).values({
      storyboardId,
      characterId,
    }).run()
  }
}

function getStoryboardCharacterIds(storyboardId: number) {
  return db.select().from(schema.storyboardCharacters)
    .where(eq(schema.storyboardCharacters.storyboardId, storyboardId)).all()
    .map(link => link.characterId)
}

function syncStoryboardProps(storyboardId: number, propIds: number[]) {
  db.delete(schema.storyboardProps)
    .where(eq(schema.storyboardProps.storyboardId, storyboardId))
    .run()

  const uniqueIds = [...new Set((propIds || []).filter(Boolean))]
  if (!uniqueIds.length) return

  for (const propId of uniqueIds) {
    db.insert(schema.storyboardProps).values({
      storyboardId,
      propId,
    }).run()
  }
}

function getStoryboardPropIds(storyboardId: number) {
  return db.select().from(schema.storyboardProps)
    .where(eq(schema.storyboardProps.storyboardId, storyboardId)).all()
    .map(link => link.propId)
}

function validateStoryboardBindings(episodeId: number, sceneId: number | null | undefined, characterIds: number[] | undefined) {
  const episodeSceneIds = new Set(
    db.select().from(schema.episodeScenes)
      .where(eq(schema.episodeScenes.episodeId, episodeId)).all()
      .map(link => link.sceneId),
  )
  const episodeCharacterIds = new Set(
    db.select().from(schema.episodeCharacters)
      .where(eq(schema.episodeCharacters.episodeId, episodeId)).all()
      .map(link => link.characterId),
  )

  if (sceneId != null && !episodeSceneIds.has(sceneId)) {
    throw new Error('scene_id 必须来自当前集已关联场景')
  }

  const invalidCharacterIds = (characterIds || []).filter(id => !episodeCharacterIds.has(id))
  if (invalidCharacterIds.length) {
    throw new Error('character_ids 必须来自当前集已关联角色')
  }
}

// POST /storyboards
app.post('/', async (c) => {
  const body = await c.req.json()
  const ts = now()
  logTaskStart('StoryboardAPI', 'create', {
    episodeId: body.episode_id,
    shotNumber: body.storyboard_number || 1,
    sceneId: body.scene_id,
    characterIds: body.character_ids,
  })
  logTaskPayload('StoryboardAPI', 'create body', body)
  validateStoryboardBindings(body.episode_id, body.scene_id, body.character_ids)
  const res = db.insert(schema.storyboards).values({
    episodeId: body.episode_id,
    storyboardNumber: body.storyboard_number || 1,
    title: body.title,
    description: body.description,
    action: body.action,
    dialogue: body.dialogue,
    sceneId: body.scene_id,
    duration: body.duration || 10,
    createdAt: ts,
    updatedAt: ts,
  }).run()
  syncStoryboardCharacters(Number(res.lastInsertRowid), body.character_ids || [])
  syncStoryboardProps(Number(res.lastInsertRowid), body.prop_ids || [])
  const [result] = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.id, Number(res.lastInsertRowid))).all()
  logTaskSuccess('StoryboardAPI', 'create', {
    storyboardId: result.id,
    episodeId: result.episodeId,
    shotNumber: result.storyboardNumber,
  })
  return created(c, {
    ...toSnakeCase(result),
    character_ids: getStoryboardCharacterIds(result.id),
    prop_ids: getStoryboardPropIds(result.id),
  })
})

// POST /storyboards/insert — Insert a storyboard above or below a reference shot
app.post('/insert', async (c) => {
  const body = await c.req.json()
  const episodeId = body.episode_id
  const referenceStoryboardId = body.reference_storyboard_id
  const direction = body.direction

  if (!episodeId || !referenceStoryboardId || !direction) {
    return badRequest(c, 'episode_id, reference_storyboard_id, and direction are required')
  }
  if (!['above', 'below'].includes(direction)) {
    return badRequest(c, 'direction must be "above" or "below"')
  }

  logTaskStart('StoryboardAPI', 'insert', {
    episodeId,
    referenceStoryboardId,
    direction,
  })

  validateStoryboardBindings(episodeId, body.scene_id, body.character_ids)

  // Find the reference storyboard
  const [refSb] = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.id, referenceStoryboardId)).all()
  if (!refSb) return badRequest(c, 'Reference storyboard not found')

  // Determine target storyboard_number
  const targetNumber = direction === 'above'
    ? refSb.storyboardNumber
    : refSb.storyboardNumber + 1

  // Shift all storyboards at or above the target position up by 1
  // Process from highest to lowest to avoid unique constraint conflicts
  const toShift = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.episodeId, episodeId)).all()
    .filter(sb => sb.storyboardNumber >= targetNumber)
    .sort((a, b) => b.storyboardNumber - a.storyboardNumber)

  const ts = now()
  for (const sb of toShift) {
    db.update(schema.storyboards)
      .set({ storyboardNumber: sb.storyboardNumber + 1, updatedAt: ts })
      .where(eq(schema.storyboards.id, sb.id))
      .run()
  }

  // Insert the new storyboard at the target position
  const res = db.insert(schema.storyboards).values({
    episodeId,
    storyboardNumber: targetNumber,
    title: body.title || `镜头${targetNumber}`,
    description: body.description || null,
    action: body.action || null,
    dialogue: body.dialogue || null,
    sceneId: body.scene_id || null,
    duration: body.duration || 10,
    createdAt: ts,
    updatedAt: ts,
  }).run()

  const newId = Number(res.lastInsertRowid)
  syncStoryboardCharacters(newId, body.character_ids || [])
  syncStoryboardProps(newId, body.prop_ids || [])

  logTaskSuccess('StoryboardAPI', 'insert', {
    storyboardId: newId,
    episodeId,
    targetNumber,
    direction,
  })

  // Return all storyboards re-sequenced
  const rows = db.select().from(schema.storyboards)
    .where(eq(schema.storyboards.episodeId, episodeId))
    .orderBy(schema.storyboards.storyboardNumber)
    .all()

  return created(c, {
    inserted_id: newId,
    storyboards: rows.map((row) => ({
      ...toSnakeCase(row),
      character_ids: getStoryboardCharacterIds(row.id),
      prop_ids: getStoryboardPropIds(row.id),
    })),
  })
})

// PUT /storyboards/:id
app.put('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  const body = await c.req.json()
  const [storyboard] = db.select().from(schema.storyboards).where(eq(schema.storyboards.id, id)).all()
  if (!storyboard) return badRequest(c, '镜头不存在')
  logTaskStart('StoryboardAPI', 'update', {
    storyboardId: id,
    episodeId: storyboard.episodeId,
    fields: Object.keys(body),
  })
  logTaskPayload('StoryboardAPI', 'update body', body)

  const fieldMap: Record<string, string> = {
    title: 'title', description: 'description', shot_type: 'shotType',
    angle: 'angle', movement: 'movement', action: 'action',
    dialogue: 'dialogue', duration: 'duration', video_prompt: 'videoPrompt',
    image_prompt: 'imagePrompt', scene_id: 'sceneId', location: 'location',
    time: 'time', atmosphere: 'atmosphere', result: 'result',
    bgm_prompt: 'bgmPrompt', sound_effect: 'soundEffect',
  }

  const updates: Record<string, any> = { updatedAt: now() }
  for (const [snakeKey, camelKey] of Object.entries(fieldMap)) {
    if (snakeKey in body) updates[camelKey] = body[snakeKey]
  }

  if ('dialogue' in body) {
    updates.ttsAudioUrl = null
    updates.subtitleUrl = null
  }

  validateStoryboardBindings(
    storyboard.episodeId,
    'scene_id' in body ? body.scene_id : storyboard.sceneId,
    'character_ids' in body ? body.character_ids : getStoryboardCharacterIds(id),
  )

  db.update(schema.storyboards).set(updates).where(eq(schema.storyboards.id, id)).run()
  if ('character_ids' in body) syncStoryboardCharacters(id, body.character_ids || [])
  if ('prop_ids' in body) syncStoryboardProps(id, body.prop_ids || [])
  logTaskSuccess('StoryboardAPI', 'update', {
    storyboardId: id,
    updatedFields: Object.keys(updates),
    characterIds: body.character_ids,
    propIds: body.prop_ids,
  })
  return success(c)
})

// POST /storyboards/:id/generate-tts
app.post('/:id/generate-tts', async (c) => {
  const id = Number(c.req.param('id'))
  const [sb] = db.select().from(schema.storyboards).where(eq(schema.storyboards.id, id)).all()
  if (!sb) return badRequest(c, '镜头不存在')
  const parsedDialogue = parseDialogueForTTS(sb.dialogue)
  if (parsedDialogue.ignorable) return badRequest(c, '该镜头没有可生成的对白或旁白')
  logTaskStart('StoryboardAPI', 'generate-tts', {
    storyboardId: id,
    episodeId: sb.episodeId,
    dialoguePreview: (sb.dialogue || '').slice(0, 40),
  })
  logTaskPayload('StoryboardAPI', 'generate-tts input', {
    storyboardId: id,
    episodeId: sb.episodeId,
    dialogue: sb.dialogue,
  })

  let voiceId = 'alloy'
  const speaker = parsedDialogue.speaker

  if (speaker) {
    if (!/^(旁白|画外音|narrator)$/i.test(speaker)) {
      const [ep] = db.select().from(schema.episodes).where(eq(schema.episodes.id, sb.episodeId)).all()
      if (ep) {
        const chars = db.select().from(schema.characters).where(eq(schema.characters.dramaId, ep.dramaId)).all()
        const found = chars.find((char) => char.name === speaker)
        if (found?.voiceStyle) voiceId = found.voiceStyle
      }
    }
  }

  const pureDialogue = parsedDialogue.pureText
  if (!pureDialogue) return badRequest(c, '未提取到可合成的文本')

  const [ep] = db.select().from(schema.episodes).where(eq(schema.episodes.id, sb.episodeId)).all()
  try {
    const audioPath = await generateTTS({ text: pureDialogue, voice: voiceId, configId: ep?.audioConfigId || null })
  db.update(schema.storyboards)
    .set({ ttsAudioUrl: audioPath, updatedAt: now() })
    .where(eq(schema.storyboards.id, id))
    .run()

    logTaskSuccess('StoryboardAPI', 'generate-tts', {
      storyboardId: id,
      voiceId,
      path: audioPath,
      textLength: pureDialogue.length,
    })
    return success(c, { tts_audio_url: audioPath, voice_id: voiceId, text: pureDialogue })
  } catch (err: any) {
    logTaskError('StoryboardAPI', 'generate-tts', { storyboardId: id, voiceId, error: err.message })
    return badRequest(c, err.message)
  }
})

// DELETE /storyboards/:id
app.delete('/:id', async (c) => {
  const id = Number(c.req.param('id'))
  logTaskStart('StoryboardAPI', 'delete', { storyboardId: id })
  db.delete(schema.storyboardCharacters).where(eq(schema.storyboardCharacters.storyboardId, id)).run()
  db.delete(schema.storyboardProps).where(eq(schema.storyboardProps.storyboardId, id)).run()
  db.delete(schema.storyboards).where(eq(schema.storyboards.id, id)).run()
  logTaskSuccess('StoryboardAPI', 'delete', { storyboardId: id })
  return success(c)
})

export default app
