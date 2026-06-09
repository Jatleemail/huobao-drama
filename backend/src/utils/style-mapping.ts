/**
 * 视觉风格映射 — 将 drama.style 映射为 AI 提示词标签
 * 贯穿全流程：角色、场景、宫格、视频
 *
 * 共 34 种风格，分为 5 组：默认、朋克系列、古典系列、现代系列、东方系列
 */
import { db, schema } from '../db/index.js'
import { eq } from 'drizzle-orm'

export type StyleCode =
  // 默认风格
  | 'realistic' | 'anime' | 'ghibli' | 'cinematic' | 'comic' | 'watercolor'
  // 朋克系列
  | 'dieselpunk' | 'solarpunk' | 'lunarpunk' | 'elfpunk' | 'astropunk'
  | 'biopunk' | 'atompunk' | 'gothpunk'
  // 古典系列
  | 'renaissance' | 'baroque' | 'rococo' | 'byzantine' | 'roman' | 'greek'
  | 'egyptian' | 'dunhuang'
  // 现代系列
  | 'popart' | 'minimalism' | 'abstract' | 'surrealism' | 'cubism'
  | 'impressionism' | 'bauhaus' | 'pixelart'
  // 东方系列
  | 'chinoiserie' | 'sumie' | 'shadowpuppet' | 'qajar'

const STYLE_EN_MAP: Record<StyleCode, string> = {
  // 默认风格
  realistic:  'photorealistic, realistic style',
  anime:      'anime style, Japanese animation',
  ghibli:     'Studio Ghibli style, hand-drawn animation',
  cinematic:  'cinematic, film-like quality',
  comic:      'comic book style, graphic novel',
  watercolor: 'watercolor painting style, artistic',
  // 朋克系列
  dieselpunk: 'dieselpunk style, industrial retro-futurism, 1930s diesel-age aesthetics',
  solarpunk:  'solarpunk style, green-tech utopia, Art Nouveau meets sustainable future',
  lunarpunk:  'lunarpunk style, moonlit mystery, bioluminescent ethereal aesthetics',
  elfpunk:    'elfpunk style, fantasy meets cyberpunk, magic-infused technology',
  astropunk:  'astropunk style, interstellar voyage, cosmic grandeur',
  biopunk:    'biopunk style, bio-tech imagination, genetic engineering aesthetics',
  atompunk:   'atompunk style, nuclear-age retro-futurism, 1950s atomic age sci-fi',
  gothpunk:   'gothpunk style, dark gothic aesthetics, grim industrial elegance',
  // 古典系列
  renaissance: 'Renaissance art style, classical humanism, Leonardo and Michelangelo inspired',
  baroque:     'Baroque art style, ornate grandeur, dramatic light and shadow, Caravaggio inspired',
  rococo:      'Rococo art style, elegant pastel aesthetics, French courtly charm',
  byzantine:   'Byzantine art style, sacred mosaics, gold-leaf divine aesthetics',
  roman:       'Ancient Roman art style, imperial grandeur, classical Roman aesthetics',
  greek:       'Ancient Greek art style, classical beauty, idealized human form',
  egyptian:    'Ancient Egyptian art style, pharaonic mystique, hieroglyphic aesthetics',
  dunhuang:    'Dunhuang mural art style, Silk Road Buddhist cave paintings, celestial apsaras',
  // 现代系列
  popart:       'pop art style, bold colors, comic-inspired pop culture aesthetics, Andy Warhol inspired',
  minimalism:   'minimalist style, less is more, clean lines and negative space',
  abstract:     'abstract expressionist style, emotional color fields, gestural brushwork',
  surrealism:   'surrealist style, dreamlike imagery, subconscious exploration, Dali and Magritte inspired',
  cubism:       'cubist style, geometric deconstruction, multi-perspective, Picasso inspired',
  impressionism:'impressionist style, light and color, fleeting moments, Monet inspired',
  bauhaus:      'Bauhaus design style, form follows function, geometric precision',
  pixelart:     'pixel art style, retro digital charm, 8-bit and 16-bit game aesthetics',
  // 东方系列
  chinoiserie:  'Chinese ink brush painting style, elegant ink wash aesthetics, traditional Chinese art',
  sumie:        'Japanese Sumi-e ink painting style, minimal brush strokes, zen aesthetics',
  shadowpuppet: 'Chinese shadow puppet style, silhouette storytelling, traditional folk art',
  qajar:        'Qajar Persian miniature art style, ornate Persian paintings, Iranian royal aesthetics',
}

const STYLE_ZH_MAP: Record<StyleCode, string> = {
  // 默认风格
  realistic:  '写实风格',
  anime:      '动漫风格',
  ghibli:     '吉卜力风格',
  cinematic:  '电影级',
  comic:      '漫画风格',
  watercolor: '水彩风格',
  // 朋克系列
  dieselpunk: '柴油朋克',
  solarpunk:  '太阳朋克',
  lunarpunk:  '月亮朋克',
  elfpunk:    '精灵朋克',
  astropunk:  '星系朋克',
  biopunk:    '生物朋克',
  atompunk:   '原子朋克',
  gothpunk:   '哥特朋克',
  // 古典系列
  renaissance: '文艺复兴',
  baroque:     '巴洛克',
  rococo:      '洛可可',
  byzantine:   '拜占庭艺术',
  roman:       '古罗马艺术',
  greek:       '古希腊艺术',
  egyptian:    '古埃及艺术',
  dunhuang:    '敦煌艺术',
  // 现代系列
  popart:       '波普艺术',
  minimalism:   '极简主义',
  abstract:     '抽象表现主义',
  surrealism:   '超现实主义',
  cubism:       '立体派',
  impressionism:'印象派',
  bauhaus:      '包豪斯',
  pixelart:     '像素艺术',
  // 东方系列
  chinoiserie:  '中国风',
  sumie:        '日本水墨画',
  shadowpuppet: '皮影戏',
  qajar:        '卡贾尔',
}

const STYLE_PORTRAIT_MAP: Record<StyleCode, string> = {
  // 默认风格
  realistic:  'photorealistic portrait',
  anime:      'anime character portrait',
  ghibli:     'Studio Ghibli character design',
  cinematic:  'cinematic character portrait',
  comic:      'comic book character art',
  watercolor: 'watercolor character illustration',
  // 朋克系列
  dieselpunk: 'dieselpunk character design',
  solarpunk:  'solarpunk character portrait',
  lunarpunk:  'lunarpunk character design',
  elfpunk:    'elfpunk character portrait',
  astropunk:  'astropunk character design',
  biopunk:    'biopunk character portrait',
  atompunk:   'atompunk character design',
  gothpunk:   'gothpunk character portrait',
  // 古典系列
  renaissance: 'Renaissance character portrait',
  baroque:     'Baroque character portrait',
  rococo:      'Rococo character portrait',
  byzantine:   'Byzantine icon portrait',
  roman:       'Roman character portrait',
  greek:       'Greek statue portrait',
  egyptian:    'Egyptian character portrait',
  dunhuang:    'Dunhuang mural character',
  // 现代系列
  popart:       'pop art character portrait',
  minimalism:   'minimalist character portrait',
  abstract:     'abstract expressionist portrait',
  surrealism:   'surrealist character portrait',
  cubism:       'cubist character portrait',
  impressionism:'impressionist character portrait',
  bauhaus:      'Bauhaus character design',
  pixelart:     'pixel art character sprite',
  // 东方系列
  chinoiserie:  'Chinese ink brush character',
  sumie:        'Sumi-e ink portrait',
  shadowpuppet: 'shadow puppet character',
  qajar:        'Qajar miniature portrait',
}

const STYLE_SCENE_MAP: Record<StyleCode, string> = {
  // 默认风格
  realistic:  'photorealistic scene',
  anime:      'anime background art',
  ghibli:     'Studio Ghibli background art',
  cinematic:  'cinematic scene',
  comic:      'comic book panel background',
  watercolor: 'watercolor landscape painting',
  // 朋克系列
  dieselpunk: 'dieselpunk industrial scene',
  solarpunk:  'solarpunk eco-city scene',
  lunarpunk:  'lunarpunk moonlit scene',
  elfpunk:    'elfpunk fantasy-tech scene',
  astropunk:  'astropunk space scene',
  biopunk:    'biopunk bio-lab scene',
  atompunk:   'atompunk atomic age scene',
  gothpunk:   'gothpunk dark scene',
  // 古典系列
  renaissance: 'Renaissance architectural scene',
  baroque:     'Baroque palace scene',
  rococo:      'Rococo garden scene',
  byzantine:   'Byzantine cathedral scene',
  roman:       'Roman city scene',
  greek:       'Greek temple scene',
  egyptian:    'Egyptian temple scene',
  dunhuang:    'Dunhuang cave art scene',
  // 现代系列
  popart:       'pop art scene',
  minimalism:   'minimalist scene',
  abstract:     'abstract expressionist scene',
  surrealism:   'surrealist dreamscape scene',
  cubism:       'cubist scene',
  impressionism:'impressionist landscape scene',
  bauhaus:      'Bauhaus architectural scene',
  pixelart:     'pixel art scene',
  // 东方系列
  chinoiserie:  'Chinese ink brush landscape',
  sumie:        'Sumi-e ink landscape',
  shadowpuppet: 'shadow puppet scene',
  qajar:        'Persian palace scene',
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
