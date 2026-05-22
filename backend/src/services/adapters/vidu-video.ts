/**
 * Vidu 视频生成 Adapter
 * 端点:
 *   img2video → POST /ent/v2/img2video (单图)
 *   start-end2video → POST /ent/v2/start-end2video (首尾帧，2图)
 *   multiframe → POST /ent/v2/multiframe (智能多帧，3+图)
 * 查询: GET /ent/v2/tasks/{id}/creations
 * 认证: Authorization: Token {apiKey} (不是 Bearer!)
 */
import type {
  VideoProviderAdapter,
  ProviderRequest,
  AIConfig,
  VideoGenerationRecord,
  VideoGenResponse,
  VideoPollResponse,
} from './types'
import { joinProviderUrl } from './url'

const VIDU_IMG2VIDEO_MODELS = [
  'viduq3-turbo', 'viduq3-pro',
  'viduq2-pro-fast', 'viduq2-pro', 'viduq2-turbo',
  'viduq1', 'viduq1-classic', 'vidu2.0',
]
const VIDU_START_END_MODELS = VIDU_IMG2VIDEO_MODELS
const VIDU_MULTIFRAME_MODELS = ['viduq2-turbo', 'viduq2-pro']

export class ViduVideoAdapter implements VideoProviderAdapter {
  provider = 'vidu'

  buildGenerateRequest(config: AIConfig, record: VideoGenerationRecord): ProviderRequest {
    // 收集所有参考图
    const images = this.collectImages(record)
    if (images.length === 0) {
      throw new Error(
        'Vidu video generation requires at least one reference image. ' +
        'Please generate a first frame or reference images first.',
      )
    }

    // 根据图片数量路由到不同端点
    if (images.length === 2 && record.referenceMode === 'first_last') {
      return this.buildStartEndRequest(config, record, images)
    }
    if (images.length >= 3 || record.referenceMode === 'multiple') {
      return this.buildMultiFrameRequest(config, record, images)
    }
    return this.buildImg2VideoRequest(config, record, images)
  }

  // ==================== img2video (单图) ====================

  private buildImg2VideoRequest(
    config: AIConfig,
    record: VideoGenerationRecord,
    images: string[],
  ): ProviderRequest {
    const model = record.model || config.model || 'viduq3-turbo'

    const body: any = {
      model,
      prompt: record.prompt,
      images,
    }

    const webhookBase = process.env.WEBHOOK_BASE_URL
    if (webhookBase) {
      body.callback_url = `${webhookBase.replace(/\/$/, '')}/webhooks/vidu`
    }
    if (record.duration) body.duration = record.duration
    if (record.aspectRatio) body.resolution = this.mapResolution(record.aspectRatio)

    return {
      url: joinProviderUrl(config.baseUrl, '', '/ent/v2/img2video'),
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Token ${config.apiKey}`,
      },
      body,
    }
  }

  // ==================== start-end2video (首尾帧) ====================

  private buildStartEndRequest(
    config: AIConfig,
    record: VideoGenerationRecord,
    images: string[],
  ): ProviderRequest {
    const model = this.pickModel(record, config, VIDU_START_END_MODELS, 'viduq3-turbo')

    const body: any = {
      model,
      images, // [firstFrame, lastFrame]
    }
    if (record.prompt) body.prompt = record.prompt
    if (record.duration) body.duration = record.duration

    const webhookBase = process.env.WEBHOOK_BASE_URL
    if (webhookBase) {
      body.callback_url = `${webhookBase.replace(/\/$/, '')}/webhooks/vidu`
    }

    return {
      url: joinProviderUrl(config.baseUrl, '', '/ent/v2/start-end2video'),
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Token ${config.apiKey}`,
      },
      body,
    }
  }

  // ==================== multiframe (智能多帧) ====================

  private buildMultiFrameRequest(
    config: AIConfig,
    record: VideoGenerationRecord,
    images: string[],
  ): ProviderRequest {
    const model = this.pickModel(record, config, VIDU_MULTIFRAME_MODELS, 'viduq2-turbo')
    const startImage = images[0]
    const keyFrames = images.slice(1)

    const imageSettings = keyFrames.map((keyImage, _i) => ({
      prompt: '',
      key_image: keyImage,
      duration: record.duration || 5,
    }))

    const body: any = {
      model,
      start_image: startImage,
      image_settings: imageSettings,
    }

    if (record.aspectRatio) body.resolution = this.mapResolution(record.aspectRatio)

    const webhookBase = process.env.WEBHOOK_BASE_URL
    if (webhookBase) {
      body.callback_url = `${webhookBase.replace(/\/$/, '')}/webhooks/vidu`
    }

    return {
      url: joinProviderUrl(config.baseUrl, '', '/ent/v2/multiframe'),
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Token ${config.apiKey}`,
      },
      body,
    }
  }

  // ==================== 公共方法 ====================

  parseGenerateResponse(result: any): VideoGenResponse {
    if (result.task_id) {
      return { isAsync: true, taskId: result.task_id }
    }
    if (result.video_url) {
      return { isAsync: false, videoUrl: result.video_url }
    }
    throw new Error('No task_id in Vidu response')
  }

  buildPollRequest(config: AIConfig, taskId: string): ProviderRequest {
    return {
      url: joinProviderUrl(config.baseUrl, '', `/ent/v2/tasks/${taskId}/creations`),
      method: 'GET',
      headers: {
        'Authorization': `Token ${config.apiKey}`,
      },
      body: undefined,
    }
  }

  parsePollResponse(result: any): VideoPollResponse {
    const state = result.state
    if (state === 'success') {
      const creations: any[] = result.creations || []
      const videoUrl = creations[0]?.url || result.video_url || null
      return { status: 'completed', videoUrl }
    }
    if (state === 'failed') {
      return {
        status: 'failed',
        error: result.err_code || result.error_msg || 'Vidu generation failed',
      }
    }
    return { status: 'processing' }
  }

  extractVideoUrl(result: any): string | null {
    const creations: any[] = result.creations || []
    return creations[0]?.url || result.video_url || result.result_url || null
  }

  static parseCallbackState(body: any): { status: 'completed' | 'failed'; videoUrl?: string; error?: string } {
    const state = body.state
    if (state === 'success') {
      const creations: any[] = body.creations || []
      return { status: 'completed', videoUrl: creations[0]?.url || body.video_url }
    }
    if (state === 'failed') {
      return { status: 'failed', error: body.err_code || body.error || 'Vidu generation failed' }
    }
    return { status: 'failed', error: `Unknown state: ${state}` }
  }

  // ==================== 内部辅助 ====================

  /** 收集所有参考图 URL，保持顺序 */
  private collectImages(record: VideoGenerationRecord): string[] {
    const images: string[] = []

    if (record.referenceMode === 'single' && record.imageUrl) {
      images.push(record.imageUrl)
    } else if (record.referenceMode === 'first_last') {
      if (record.firstFrameUrl) images.push(record.firstFrameUrl)
      if (record.lastFrameUrl) images.push(record.lastFrameUrl)
    } else if (record.referenceMode === 'multiple' && record.referenceImageUrls) {
      try {
        const refs = JSON.parse(record.referenceImageUrls)
        images.push(...refs)
      } catch { /* ignore */ }
    }

    return images
  }

  /** 从允许列表中选取模型，确保不会传入不支持的模型到特定端点 */
  private pickModel(
    record: VideoGenerationRecord,
    config: AIConfig,
    allowed: string[],
    fallback: string,
  ): string {
    const candidate = record.model || config.model || fallback
    return allowed.includes(candidate) ? candidate : fallback
  }

  private mapResolution(aspectRatio: string): string {
    const ratioMap: Record<string, string> = {
      '16:9': '720p',
      '9:16': '720p',
      '1:1': '720p',
    }
    return ratioMap[aspectRatio] || '720p'
  }
}
