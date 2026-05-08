import { toast } from 'vue-sonner'
import { api } from './useApi'

const AGENT_TIMEOUT_MS = 600_000 // 10 minutes

export function useAgent() {
  const running = ref(false)
  const runningType = ref<string | null>(null)

  async function run(type: string, msg: string, dramaId: number, episodeId: number, onDone?: () => void) {
    if (running.value) { toast.warning('操作执行中'); return }
    running.value = true
    runningType.value = type
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), AGENT_TIMEOUT_MS)
    try {
      await api.post<any>(`/agent/${type}/chat`, {
        message: msg,
        drama_id: dramaId,
        episode_id: episodeId,
      }, { signal: controller.signal })
      toast.success('完成')
      onDone?.()
    } catch (err: any) {
      if (err.name === 'AbortError') {
        toast.error('AI 处理超时，请重试或减少内容量')
      } else {
        toast.error(err.message)
      }
    } finally {
      clearTimeout(timeoutId)
      running.value = false
      runningType.value = null
    }
  }

  return { running, runningType, run }
}
