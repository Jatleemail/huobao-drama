import { describe, it, expect, vi } from 'vitest'
import { useAgentStream } from '../useAgentStream'

function sseStream(events: Array<{ type: string; payload: Record<string, unknown> }>) {
  const body = events.map((e) => `data: ${JSON.stringify(e)}\n\n`).join('')
  return new ReadableStream({
    start(controller) {
      controller.enqueue(new TextEncoder().encode(body))
      controller.close()
    },
  })
}

function mockFetch(stream: ReadableStream<Uint8Array>, ok = true, status = 200, errorJson?: string) {
  return vi.fn().mockResolvedValue({
    ok,
    status,
    text: async () => errorJson || '',
    body: stream,
  })
}

describe('useAgentStream — SSE event handling', () => {
  it('step-start sets currentStep', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'step-start', payload: { stepNumber: 3 } },
      { type: 'finish', payload: { text: '', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.currentStep).toBe(3)
    vi.unstubAllGlobals()
  })

  it('tool-call pushes active step with Chinese label', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'tool-call', payload: { toolName: 'read_storyboard_context', toolCallId: '1' } },
      { type: 'finish', payload: { text: 'done', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.steps).toHaveLength(1)
    expect(progress.value.steps[0].toolName).toBe('read_storyboard_context')
    expect(progress.value.steps[0].label).toBe('读取剧本与角色场景')
    expect(progress.value.steps[0].status).toBe('active')
    vi.unstubAllGlobals()
  })

  it('tool-result sets matching active step to done', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'tool-call', payload: { toolName: 'read_storyboard_context', toolCallId: 'call_1' } },
      { type: 'tool-result', payload: { toolName: 'read_storyboard_context', toolCallId: 'call_1', result: '3 角色, 2 场景' } },
      { type: 'finish', payload: { text: 'done', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.steps[0].status).toBe('done')
    expect(progress.value.steps[0].result).toBeTruthy()
    vi.unstubAllGlobals()
  })

  it('text-delta accumulates text; finish replaces with final text', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'text-delta', payload: { text: 'Hello ' } },
      { type: 'text-delta', payload: { text: 'World' } },
      { type: 'finish', payload: { text: 'Hello World — final', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.text).toBe('Hello World — final')
    vi.unstubAllGlobals()
  })

  it('step-finish sets totalSteps', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'step-finish', payload: { stepNumber: 1, totalSteps: 20, usage: {} } },
      { type: 'finish', payload: { text: '', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.totalSteps).toBe(20)
    vi.unstubAllGlobals()
  })

  it('error sets status: error and preserves error field', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'error', payload: { error: 'API key invalid' } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.status).toBe('error')
    expect(progress.value.error).toBe('API key invalid')
    vi.unstubAllGlobals()
  })

  it('unknown event type is no-op (status becomes done)', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'unknown-type', payload: { foo: 'bar' } },
      { type: 'finish', payload: { text: '', toolCalls: [], toolResults: [] } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.status).toBe('done')
    vi.unstubAllGlobals()
  })

  it('finish sets summary from tool calls', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      {
        type: 'finish',
        payload: {
          text: 'final text',
          toolCalls: [{ toolName: 'read_storyboard_context' }, { toolName: 'save_storyboards' }],
          toolResults: [],
        },
      },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.status).toBe('done')
    expect(progress.value.text).toBe('final text')
    expect(progress.value.summary).toContain('完成')
    expect(progress.value.summary).toContain('read_storyboard_context')
    vi.unstubAllGlobals()
  })
})

describe('useAgentStream — SSE parser edge cases', () => {
  it('stream ends without finish event → connection lost error', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = sseStream([
      { type: 'step-start', payload: { stepNumber: 1 } },
    ])
    vi.stubGlobal('fetch', mockFetch(stream))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.status).toBe('error')
    expect(progress.value.error).toContain('连接中断')
    vi.unstubAllGlobals()
  })

  it('HTTP 400 response triggers error path', async () => {
    const { progress, runStream } = useAgentStream()
    const stream = new ReadableStream({ start(c) { c.close() } })
    vi.stubGlobal('fetch', mockFetch(stream, false, 400, JSON.stringify({ message: 'Invalid agent type' })))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.status).toBe('error')
    expect(progress.value.error).toContain('Invalid agent type')
    vi.unstubAllGlobals()
  })

  it('AbortError shows timeout message', async () => {
    const { progress, runStream } = useAgentStream()
    const error = new Error('Aborted')
    error.name = 'AbortError'
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(error))
    await runStream('storyboard_breaker', 'test', 1, 1)
    expect(progress.value.error).toContain('超时')
    vi.unstubAllGlobals()
  })
})
