/**
 * 视觉风格映射 — 将 drama.style 映射为 AI 提示词标签
 * 贯穿全流程：角色、场景、宫格、视频
 */
import { db, schema } from '../db/index.js'
import { eq } from 'drizzle-orm'

export type StyleCode = 'realistic' | 'anime' | 'ghibli' | 'cinematic' | 'comic' | 'watercolor'

const STYLE_EN_MAP: Record<StyleCode, string> = {
  realistic: 'photorealistic, realistic style',
  anime: 'anime style, Japanese animation',
  ghibli: 'Studio Ghibli style, hand-drawn animation',
  cinematic: 'cinematic, film-like quality',
  comic: 'comic book style, graphic novel',
  watercolor: 'watercolor painting style, artistic',
}

const STYLE_ZH_MAP: Record<StyleCode, string> = {
  realistic: '写实风格',
  anime: '动漫风格',
  ghibli: '吉卜力风格',
  cinematic: '电影级',
  comic: '漫画风格',
  watercolor: '水彩风格',
}

const STYLE_PORTRAIT_MAP: Record<StyleCode, string> = {
  realistic: 'photorealistic portrait',
  anime: 'anime character portrait',
  ghibli: 'Studio Ghibli character design',
  cinematic: 'cinematic character portrait',
  comic: 'comic book character art',
  watercolor: 'watercolor character illustration',
}

const STYLE_SCENE_MAP: Record<StyleCode, string> = {
  realistic: 'photorealistic scene',
  anime: 'anime background art',
  ghibli: 'Studio Ghibli background art',
  cinematic: 'cinematic scene',
  comic: 'comic book panel background',
  watercolor: 'watercolor landscape painting',
}

/** Type guard for valid style codes */
export function isValidStyle(s: string): s is StyleCode {
  return s in STYLE_EN_MAP
}

function resolveStyle(style?: string | null): StyleCode {
  return style && isValidStyle(style) ? style : 'cinematic'
}

/** Get English style tags for general prompts */
export function styleEnTag(style?: string | null): string {
  return STYLE_EN_MAP[resolveStyle(style)]
}

/** Get Chinese style labels */
export function styleZhLabel(style?: string | null): string {
  return STYLE_ZH_MAP[resolveStyle(style)]
}

/** Get style tag for character/portrait prompts */
export function stylePortrait(style?: string | null): string {
  return STYLE_PORTRAIT_MAP[resolveStyle(style)]
}

/** Get style tag for scene/background prompts */
export function styleScene(style?: string | null): string {
  return STYLE_SCENE_MAP[resolveStyle(style)]
}

/** Quality suffix for all image prompts (without style tag — callers compose with their own style tag) */
export function qualitySuffix(): string {
  return 'high quality, consistent art style, no text, no watermark'
}

/** Load drama style from DB; returns undefined when drama not found */
export function getDramaStyle(dramaId: number): string | undefined {
  if (!dramaId) return undefined
  const [drama] = db.select().from(schema.dramas).where(eq(schema.dramas.id, dramaId)).all()
  return drama?.style || undefined
}

// ─── 视频比例 ───────────────────────────────────────

export type AspectRatio = '16:9' | '9:16'

/** Convert aspect ratio to image generation size string */
export function aspectRatioToSize(aspectRatio?: string | null): string {
  return aspectRatio === '9:16' ? '1080x1920' : '1920x1080'
}

/** Get grid cell pixel dimensions for a given aspect ratio */
export function getGridCellSize(aspectRatio?: string | null): { w: number; h: number } {
  return aspectRatio === '9:16' ? { w: 540, h: 960 } : { w: 960, h: 540 }
}

/** Load drama aspect ratio from DB */
export function getDramaAspectRatio(dramaId: number): string {
  if (!dramaId) return '16:9'
  const [drama] = db.select().from(schema.dramas).where(eq(schema.dramas.id, dramaId)).all()
  return drama?.aspectRatio || '16:9'
}

/** Load drama aspect ratio and style together (single DB query) */
export function getDramaVisualSettings(dramaId: number): { aspectRatio: string; style?: string } {
  if (!dramaId) return { aspectRatio: '16:9', style: undefined }
  const [drama] = db.select().from(schema.dramas).where(eq(schema.dramas.id, dramaId)).all()
  return {
    aspectRatio: drama?.aspectRatio || '16:9',
    style: drama?.style || undefined,
  }
}
