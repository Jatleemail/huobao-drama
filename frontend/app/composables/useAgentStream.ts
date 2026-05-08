import { ref } from 'vue'

interface StepEntry {
  toolName: string
  label: string
  status: 'pending' | 'active' | 'done' | 'error'
  result?: string
}

interface AgentProgress {
  steps: StepEntry[]
  text: string
  status: 'idle' | 'running' | 'done' | 'error'
  currentStep?: number
  totalSteps?: number
  error?: string
  summary?: string
}

const AGENT_TIMEOUT_MS = 900_000 // 15 minutes

const TOOL_LABELS: Record<string, string> = {
  read_storyboard_context: '读取剧本与角色场景',
  save_storyboards: '保存分镜',
  update_storyboard: '更新分镜',
  generate_grid_prompt: '生成宫格提示词',
  read_episode_script: '读取剧集内容',
  save_script: '保存改写剧本',
  read_script_for_extraction: '读取剧本',
  read_existing_characters: '读取已有角色',
  read_existing_scenes: '读取已有场景',
  save_dedup_characters: '保存角色（去重）',
  save_dedup_scenes: '保存场景（去重）',
  list_voices: '获取音色列表',
  get_characters: '获取角色列表',
  assign_voice: '分配音色',
  read_characters: '读取角色信息',
  read_scenes: '读取场景信息',
  read_shots_for_grid: '读取分镜信息',
}

export function useAgentStream() {
  const progress = ref<AgentProgress>({ steps: [], text: '', status: 'idle' })
  let finished = false

  function summarizeResult(payload: Record<string, unknown>): string {
    if (payload.result) {
      const r = payload.result as Record<string, unknown>
      if (typeof r === 'string') return (r as string).slice(0, 200)
      if (typeof r.message === 'string') return r.message as string
      if (typeof r.count === 'number') return `${r.count} 条记录`
    }
    return '完成'
  }

  function buildSummary(payload: Record<string, unknown>): string {
    const toolCalls = (payload.toolCalls as Array<{ toolName?: string }>) || []
    const toolNames = toolCalls.map((tc) => tc.toolName).filter(Boolean).join(', ')
    return toolNames ? `完成 · 调用: ${toolNames}` : '完成'
  }

  function handleEvent(event: { type: string; payload: Record<string, unknown> }) {
    switch (event.type) {
      case 'step-start':
        progress.value.currentStep = event.payload.stepNumber as number
        break
      case 'tool-call':
        progress.value.steps.push({
          toolName: event.payload.toolName as string,
          label: TOOL_LABELS[event.payload.toolName as string] || (event.payload.toolName as string),
          status: 'active',
        })
        break
      case 'tool-result': {
        const activeStep = progress.value.steps.find((s) => s.status === 'active')
        if (activeStep) {
          activeStep.status = 'done'
          activeStep.result = summarizeResult(event.payload)
        }
        break
      }
      case 'text-delta':
        progress.value.text += event.payload.text as string
        break
      case 'step-finish':
        progress.value.totalSteps = event.payload.totalSteps as number
        break
      case 'finish':
        finished = true
        progress.value.text = event.payload.text as string
        progress.value.summary = buildSummary(event.payload)
        break
      case 'error':
        progress.value.status = 'error'
        progress.value.error = event.payload.error as string
        break
    }
  }

  async function runStream(
    type: string,
    msg: string,
    dramaId: number,
    episodeId: number,
  ): Promise<void> {
    progress.value = { steps: [], text: '', status: 'running' }
    finished = false
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), AGENT_TIMEOUT_MS)
    try {
      const response = await fetch(`/api/v1/agent/${type}/chat-stream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: msg, drama_id: dramaId, episode_id: episodeId }),
        signal: controller.signal,
      })
      if (!response.ok) {
        const body = await response.text()
        let message = `${response.status}`
        try { message = JSON.parse(body).message || message } catch {}
        throw new Error(message)
      }
      const reader = response.body!.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      while (true) {
        const { done, value } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() || ''
        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const event = JSON.parse(line.slice(6))
            handleEvent(event)
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        progress.value.error = 'AI 处理超时（超过 15 分钟），请重试'
      } else {
        progress.value.error = err instanceof Error ? err.message : 'SSE 连接异常'
      }
      progress.value.status = 'error'
      return
    } finally {
      clearTimeout(timeoutId)
    }
    if (!finished && progress.value.status !== 'error') {
      progress.value.status = 'error'
      progress.value.error = '连接中断：服务器未发送完成事件'
      return
    }
    if (progress.value.status !== 'error') {
      progress.value.status = 'done'
    }
  }

  return { progress, runStream }
}
