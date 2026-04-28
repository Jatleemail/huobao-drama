/**
 * Vidu 图片生成 Adapter
 * 端点: /ent/v2/reference2image
 * 认证: Authorization: Token {apiKey} (不是 Bearer!)
 * 模型: viduq2 (文生图+参考生图), viduq1 (仅参考生图)
 */
import type { ImageProviderAdapter, ImageGenerationRecord, AIConfig } from './types'
import { joinProviderUrl } from './url'

export class ViduImageAdapter implements ImageProviderAdapter {
  readonly provider = 'vidu'

  buildGenerateRequest(config: AIConfig, record: ImageGenerationRecord): {
    url: string
    method: string
    headers: Record<string, string>
    body: any
  } {
    const model = record.model || config.model || 'viduq2'
    const body: any = {
      model,
      prompt: record.prompt || '',
    }

    // 参考图：支持 URL 或 base64
    if (record.referenceImages) {
      try {
        const refs = JSON.parse(record.referenceImages)
        if (Array.isArray(refs) && refs.length > 0) {
          body.images = refs.slice(0, 7)
        }
      } catch {}
    }

    // 比例映射：从 size 参数提取
    if (record.size) {
      body.aspect_ratio = this.sizeToAspectRatio(record.size)
    } else {
      body.aspect_ratio = '16:9'
    }

    body.resolution = '1080p'
    body.seed = Math.floor(Math.random() * 2147483647)

    return {
      url: joinProviderUrl(config.baseUrl, '', '/ent/v2/reference2image'),
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Token ${config.apiKey}`,
      },
      body,
    }
  }

  parseGenerateResponse(result: any): {
    isAsync: boolean
    taskId?: string
    imageUrl?: string
  } {
    if (result.task_id) {
      return { isAsync: true, taskId: result.task_id }
    }
    // 同步返回（不太可能，但保留处理）
    if (result.image_url) {
      return { isAsync: false, imageUrl: result.image_url }
    }
    throw new Error(`Vidu image response missing task_id: ${JSON.stringify(result).slice(0, 200)}`)
  }

  buildPollRequest(config: AIConfig, taskId: string): {
    url: string
    method: string
    headers: Record<string, string>
    body: any
  } {
    return {
      url: joinProviderUrl(config.baseUrl, '', `/ent/v2/tasks/${taskId}/creations`),
      method: 'GET',
      headers: {
        'Authorization': `Token ${config.apiKey}`,
      },
      body: undefined,
    }
  }

  parsePollResponse(result: any): {
    status: 'pending' | 'processing' | 'completed' | 'failed'
    imageUrl?: string
    error?: string
  } {
    const state = result.state

    if (state === 'success') {
      const imageUrl = this.extractImageUrl(result)
      return { status: 'completed', imageUrl: imageUrl || undefined }
    }

    if (state === 'failed') {
      return { status: 'failed', error: result.error || result.message || 'Vidu generation failed' }
    }

    // created / queueing / processing
    return { status: 'processing' }
  }

  extractImageUrl(result: any): string | null {
    // Vidu 查询任务返回格式: creations[0].url
    if (result.creations?.[0]?.url) return result.creations[0].url
    if (result.creations?.[0]?.cover_url) return result.creations[0].cover_url
    // 兼容其他可能的返回格式
    if (result.image_url) return result.image_url
    if (result.output?.image_url) return result.output.image_url
    if (result.results?.[0]?.url) return result.results[0].url
    return null
  }

  extractImageBase64(result: any): { data: string; mimeType: string } | null {
    return null
  }

  private sizeToAspectRatio(size: string): string {
    const [w, h] = size.split('x').map(Number)
    if (w && h) {
      const ratio = w / h
      if (ratio > 1.7) return '16:9'
      if (ratio < 0.75) return '9:16'
      if (ratio > 0.9 && ratio < 1.1) return '1:1'
      if (ratio > 1.2 && ratio < 1.4) return '4:3'
      if (ratio > 0.7 && ratio < 0.8) return '3:4'
    }
    return '16:9'
  }
}
