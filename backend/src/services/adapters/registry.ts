/**
 * Provider Adapter 注册表
 * 根据 provider 名称返回对应的 Adapter 实例
 */
import { MiniMaxImageAdapter } from './minimax-image'
import { MiniMaxVideoAdapter } from './minimax-video'
import { MiniMaxTTSAdapter } from './minimax-tts'
import { OpenAIImageAdapter } from './openai-image'
import { GeminiImageAdapter } from './gemini-image'
import { VolcEngineImageAdapter } from './volcengine-image'
import { VolcEngineVideoAdapter } from './volcengine-video'
import { ViduVideoAdapter } from './vidu-video'
import { ViduImageAdapter } from './vidu-image'
import { AliImageAdapter } from './ali-image'
import { AliVideoAdapter } from './ali-video'
import type { ImageProviderAdapter, VideoProviderAdapter, TTSProviderAdapter } from './types'

// 图片 Adapter 注册表
export const imageAdapters: Record<string, ImageProviderAdapter> = {
  minimax: new MiniMaxImageAdapter(),
  openai: new OpenAIImageAdapter(),
  gemini: new GeminiImageAdapter(),
  volcengine: new VolcEngineImageAdapter(),
  ali: new AliImageAdapter(),
  vidu: new ViduImageAdapter(),
  // Chatfire - 待确认 API 格式，暂用 OpenAI
  chatfire: new OpenAIImageAdapter(),
}

// 视频 Adapter 注册表
export const videoAdapters: Record<string, VideoProviderAdapter> = {
  minimax: new MiniMaxVideoAdapter(),
  volcengine: new VolcEngineVideoAdapter(),
  vidu: new ViduVideoAdapter(),
  ali: new AliVideoAdapter(),
  // Chatfire 视频 - 待确认 API 格式
}

// TTS Adapter 注册表
export const ttsAdapters: Record<string, TTSProviderAdapter> = {
  minimax: new MiniMaxTTSAdapter(),
}

export function getTTSAdapter(provider: string): TTSProviderAdapter {
  const key = provider.toLowerCase()
  const adapter = ttsAdapters[key]
  if (!adapter) {
    throw new Error(
      `Provider "${provider}" 不支持 TTS。支持的 TTS provider: ${Object.keys(ttsAdapters).join(', ')}。`
    )
  }
  return adapter
}

/** 支持的图片 provider 列表（用于错误提示） */
export const SUPPORTED_IMAGE_PROVIDERS = Object.keys(imageAdapters)

/**
 * 获取图片 Adapter
 * @param provider 厂商名称
 * @returns 对应的 Adapter
 * @throws 如果 provider 不支持图片生成
 */
export function getImageAdapter(provider: string): ImageProviderAdapter {
  const key = provider.toLowerCase()
  const adapter = imageAdapters[key]
  if (!adapter) {
    throw new Error(
      `Provider "${provider}" 不支持图片生成。支持的图片 provider: ${SUPPORTED_IMAGE_PROVIDERS.join(', ')}。` +
      `请前往设置页面将图片服务的 provider 修改为以上支持的厂商。`
    )
  }
  return adapter
}

/**
 * 获取视频 Adapter
 * @param provider 厂商名称
 * @returns 对应的 Adapter
 * @throws 如果 provider 不支持视频生成
 */
export function getVideoAdapter(provider: string): VideoProviderAdapter {
  const key = provider.toLowerCase()
  const adapter = videoAdapters[key]
  if (!adapter) {
    throw new Error(
      `Provider "${provider}" 不支持视频生成。支持的视频 provider: ${Object.keys(videoAdapters).join(', ')}。`
    )
  }
  return adapter
}
