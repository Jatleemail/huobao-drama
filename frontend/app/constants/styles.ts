/**
 * 视觉风格常量 — 34 种风格，分组展示，含中文描述（tooltip）
 *
 * 与后端 style-mapping.ts 保持同步
 */

export interface StyleOption {
  label: string
  value: string
  description: string
}

export interface StyleCategory {
  label: string
  options: StyleOption[]
}

const DEFAULT_STYLES: StyleOption[] = [
  { label: '写实', value: 'realistic',  description: '真实摄影风格，逼真写实的画面质感，还原现实世界的视觉效果' },
  { label: '动漫', value: 'anime',      description: '日式动画风格，日本动漫的标志性画风，富有表现力的角色与场景' },
  { label: '吉卜力', value: 'ghibli',     description: '吉卜力风格，宫崎骏工作室标志性的手绘动画美学，温暖治愈' },
  { label: '电影级', value: 'cinematic',  description: '电影级画质，电影般的质感与光影效果，兼具氛围感与故事感' },
  { label: '漫画', value: 'comic',      description: '美式漫画风格，漫画书与图像小说的视觉语言，粗线条与高对比' },
  { label: '水彩', value: 'watercolor', description: '水彩绘画风格，水彩画特有的晕染与透明质感，柔和而富有诗意' },
]

const PUNK_STYLES: StyleOption[] = [
  { label: '柴油朋克', value: 'dieselpunk', description: '柴油朋克 — 工业时代的力量感，1930-50年代柴油机美学，铁与火的复古未来' },
  { label: '太阳朋克', value: 'solarpunk',  description: '太阳朋克 — 绿色科技乌托邦，新艺术运动与可持续未来的交融，光明与希望' },
  { label: '月亮朋克', value: 'lunarpunk',  description: '月亮朋克 — 神秘月光下的浪漫，生物荧光与空灵美学的结合，幽暗而唯美' },
  { label: '精灵朋克', value: 'elfpunk',    description: '精灵朋克 — 奇幻与科技的碰撞，魔法注入赛博格世界，精灵美学与机械共存' },
  { label: '星系朋克', value: 'astropunk',  description: '星系朋克 — 星际漫游的壮丽，宇宙尺度的宏大视觉，星辰大海的浪漫' },
  { label: '生物朋克', value: 'biopunk',    description: '生物朋克 — 生命科技的遐想，基因工程与生物改造的美学，有机与机械的交融' },
  { label: '原子朋克', value: 'atompunk',   description: '原子朋克 — 核时代的复古未来，1950年代原子能科幻想象，辐射与希望并存' },
  { label: '哥特朋克', value: 'gothpunk',   description: '哥特朋克 — 暗黑美学的极致，哥特式黑暗优雅与工业粗粝感的融合' },
]

const CLASSICAL_STYLES: StyleOption[] = [
  { label: '文艺复兴', value: 'renaissance', description: '文艺复兴 — 人文主义的光辉，达芬奇与米开朗基罗时代的古典美学，理性与美的统一' },
  { label: '巴洛克', value: 'baroque',     description: '巴洛克 — 华丽繁复的极致，戏剧性的光影对比，卡拉瓦乔式的强烈明暗' },
  { label: '洛可可', value: 'rococo',      description: '洛可可 — 优雅柔美的宫廷风，粉彩色调与精致装饰，法式浪漫与闲适' },
  { label: '拜占庭艺术', value: 'byzantine',   description: '拜占庭艺术 — 神圣庄严的宗教美，金色马赛克镶嵌画，圣像与穹顶的辉煌' },
  { label: '古罗马艺术', value: 'roman',       description: '古罗马艺术 — 永恒之城的辉煌，帝国的宏伟与庄严，古典罗马的视觉遗产' },
  { label: '古希腊艺术', value: 'greek',       description: '古希腊艺术 — 理性与美的统一，理想化的人体比例，神庙与雕塑的完美' },
  { label: '古埃及艺术', value: 'egyptian',    description: '古埃及艺术 — 神秘的法老文明，象形文字与墓葬壁画，庄严的正面律' },
  { label: '敦煌艺术', value: 'dunhuang',    description: '敦煌艺术 — 丝路明珠的璀璨，千年佛教壁画与飞天，东西方艺术的交汇' },
]

const MODERN_STYLES: StyleOption[] = [
  { label: '波普艺术', value: 'popart',       description: '波普艺术 — 大众文化的狂欢，安迪·沃霍尔式的大胆色彩与流行符号，消费社会的镜像' },
  { label: '极简主义', value: 'minimalism',   description: '极简主义 — 少即是多的哲学，干净线条与留白空间，纯粹的形式美学' },
  { label: '抽象表现主义', value: 'abstract',     description: '抽象表现主义 — 情感的自由表达，色彩与笔触的激情宣泄，行动绘画的张力' },
  { label: '超现实主义', value: 'surrealism',   description: '超现实主义 — 梦境与现实的交织，达利与马格利特式的奇幻意象，潜意识的探索' },
  { label: '立体派', value: 'cubism',       description: '立体派 — 多角度的视觉解构，毕加索式的几何分解与重组，破碎而有序' },
  { label: '印象派', value: 'impressionism', description: '印象派 — 光影瞬间的捕捉，莫奈式的色彩与光线，朦胧而充满氛围' },
  { label: '包豪斯', value: 'bauhaus',      description: '包豪斯 — 功能与美的统一，几何精确与工业设计美学，形式服从功能' },
  { label: '像素艺术', value: 'pixelart',     description: '像素艺术 — 数字时代的复古情怀，8-bit与16-bit游戏美学，像素颗粒的魅力' },
]

const EASTERN_STYLES: StyleOption[] = [
  { label: '中国风', value: 'chinoiserie',  description: '中国风 — 东方雅致之美，传统水墨与工笔的意境，留白与写意的神韵' },
  { label: '日本水墨画', value: 'sumie',        description: '日本水墨画 — 虚实相生的意境，极简笔触与禅意美学，墨色浓淡间的哲理' },
  { label: '皮影戏', value: 'shadowpuppet', description: '皮影戏 — 光影中的故事，传统民间剪影艺术，镂空与灯光的戏剧张力' },
  { label: '卡贾尔', value: 'qajar',        description: '卡贾尔 — 波斯艺术的辉煌，伊朗卡扎尔王朝细密画，华丽装饰与宫廷美学' },
]

/** 分组后的风格选项列表，供 BaseSelect 直接使用 */
export const STYLE_CATEGORIES: StyleCategory[] = [
  { label: '默认风格', options: DEFAULT_STYLES },
  { label: '朋克系列', options: PUNK_STYLES },
  { label: '古典系列', options: CLASSICAL_STYLES },
  { label: '现代系列', options: MODERN_STYLES },
  { label: '东方系列', options: EASTERN_STYLES },
]

/** 短标签映射表（value → label），供卡片 badge 等场景使用 */
export const STYLE_SHORT_LABELS: Record<string, string> = Object.fromEntries(
  STYLE_CATEGORIES.flatMap(c => c.options).map(o => [o.value, o.label]),
)
