/**
 * Agent 聊天路由 — 非流式版本
 */
import { Hono } from 'hono'
import { createAgent, validAgentTypes } from '../agents/index.js'
import { success, badRequest } from '../utils/response.js'
import { logTaskError, logTaskPayload, logTaskProgress, logTaskStart, logTaskSuccess } from '../utils/task-logger.js'

const app = new Hono()

function normalizeToolName(entry: any) {
  return entry?.toolName
    || entry?.tool?.toolName
    || entry?.tool?.id
    || entry?.name
    || entry?.type
    || null
}

function normalizeToolResult(entry: any) {
  const result = entry?.result ?? entry?.output ?? entry?.data ?? null
  return typeof result === 'string' ? result : JSON.stringify(result)
}

function extractStreamPayload(chunk: any): Record<string, unknown> {
  switch (chunk.type) {
    case 'step-start':
      return { stepNumber: chunk.payload?.stepNumber }
    case 'step-finish':
      return {
        stepNumber: chunk.payload?.stepNumber,
        totalSteps: chunk.payload?.totalSteps,
        usage: chunk.payload?.usage,
      }
    case 'tool-call':
      return {
        toolCallId: chunk.payload?.toolCallId,
        toolName: chunk.payload?.toolName,
        args: chunk.payload?.args,
      }
    case 'tool-result':
      return {
        toolCallId: chunk.payload?.toolCallId,
        toolName: chunk.payload?.toolName,
        result: typeof chunk.payload?.result === 'string'
          ? chunk.payload.result
          : JSON.stringify(chunk.payload?.result).slice(0, 500),
      }
    case 'text-delta':
      return { text: chunk.payload?.text }
    case 'finish':
      return {
        text: chunk.payload?.text,
        toolCalls: chunk.payload?.toolCalls?.map((tc: any) => ({
          toolCallId: tc.toolCallId,
          toolName: tc.toolName,
          args: tc.args,
        })),
        toolResults: chunk.payload?.toolResults?.map((tr: any) => ({
          toolCallId: tr.toolCallId,
          toolName: tr.toolName,
          result: typeof tr.result === 'string' ? tr.result.slice(0, 500) : JSON.stringify(tr.result).slice(0, 500),
        })),
        usage: chunk.payload?.usage,
      }
    case 'error':
      return { error: chunk.payload?.error || 'Unknown error' }
    default:
      return {}
  }
}

// POST /agent/:type/chat — 非流式 Agent 对话
app.post('/:type/chat', async (c) => {
  const agentType = c.req.param('type')
  if (!validAgentTypes.includes(agentType)) {
    return badRequest(c, `Invalid agent type: ${agentType}`)
  }

  const body = await c.req.json()
  const { message, drama_id, episode_id } = body

  logTaskStart('Agent', agentType, {
    dramaId: drama_id,
    episodeId: episode_id,
    message,
  })
  logTaskPayload('Agent', `${agentType} input`, body)

  if (!episode_id || !drama_id) {
    logTaskError('Agent', agentType, { reason: 'missing drama_id or episode_id' })
    return badRequest(c, 'drama_id and episode_id are required')
  }

  const agent = createAgent(agentType, episode_id, drama_id)
  if (!agent) {
    logTaskError('Agent', agentType, { reason: 'agent not found' })
    return badRequest(c, 'Agent not found')
  }

  const startTime = performance.now()

  try {
    const result = await agent.generate(
      [{ role: 'user', content: message }],
      { maxSteps: 20 },
    )

    const elapsed = ((performance.now() - startTime) / 1000).toFixed(1)
    logTaskSuccess('Agent', agentType, { elapsedSeconds: elapsed })

    // 收集所有 tool calls 和 results
    const toolCalls = result.toolCalls || []
    const toolResults = result.toolResults || []
    const normalizedToolCalls = toolCalls.map((tc: any) => ({
      toolName: normalizeToolName(tc),
      args: tc?.args ?? tc?.input ?? null,
    }))
    const normalizedToolResults = toolResults.map((tr: any) => ({
      toolName: normalizeToolName(tr),
      result: normalizeToolResult(tr),
    }))

    logTaskProgress('Agent', 'tool-summary', {
      agentType,
      toolCalls: normalizedToolCalls.map((tc: any) => tc.toolName),
      toolResults: normalizedToolResults.map((tr: any) => tr.toolName),
    })
    logTaskPayload('Agent', `${agentType} tool-results`, normalizedToolResults)

    return success(c, {
      type: 'done',
      text: result.text || '',
      toolCalls: normalizedToolCalls,
      toolResults: normalizedToolResults,
    })
  } catch (err: any) {
    const elapsed = ((performance.now() - startTime) / 1000).toFixed(1)
    logTaskError('Agent', agentType, { elapsedSeconds: elapsed, error: err.message })
    console.error(err.stack || err)
    return badRequest(c, err.message || 'Agent execution failed')
  }
})

// POST /agent/:type/chat-stream — SSE streaming Agent chat
app.post('/:type/chat-stream', async (c) => {
  const agentType = c.req.param('type')
  if (!validAgentTypes.includes(agentType)) {
    return badRequest(c, `Invalid agent type: ${agentType}`)
  }

  const body = await c.req.json()
  const { message, drama_id, episode_id } = body

  logTaskStart('Agent', `${agentType}-stream`, {
    dramaId: drama_id,
    episodeId: episode_id,
    message,
  })

  if (!episode_id || !drama_id) {
    logTaskError('Agent', `${agentType}-stream`, { reason: 'missing drama_id or episode_id' })
    return badRequest(c, 'drama_id and episode_id are required')
  }

  const agent = createAgent(agentType, episode_id, drama_id)
  if (!agent) {
    logTaskError('Agent', `${agentType}-stream`, { reason: 'agent not found' })
    return badRequest(c, 'Agent not found')
  }

  const startTime = performance.now()

  const stream = new ReadableStream({
    async start(controller) {
      try {
        const output = await agent.stream(
          [{ role: 'user', content: message }],
          {
            maxSteps: 20,
            abortSignal: c.req.raw.signal,
            onError: ({ error }) => {
              const event = JSON.stringify({ type: 'error', payload: { error: error instanceof Error ? error.message : String(error) } })
              controller.enqueue(`data: ${event}\n\n`)
            },
          },
        )

        const reader = output.fullStream.getReader()
        while (true) {
          const { done, value } = await reader.read()
          if (done) break
          const event = JSON.stringify({
            type: value.type,
            payload: extractStreamPayload(value),
          })
          controller.enqueue(`data: ${event}\n\n`)
        }
        reader.releaseLock()

        const elapsed = ((performance.now() - startTime) / 1000).toFixed(1)
        logTaskSuccess('Agent', `${agentType}-stream`, { elapsedSeconds: elapsed })
        controller.close()
      } catch (err: any) {
        const elapsed = ((performance.now() - startTime) / 1000).toFixed(1)
        logTaskError('Agent', `${agentType}-stream`, { elapsedSeconds: elapsed, error: err.message })
        const event = JSON.stringify({ type: 'error', payload: { error: err.message || 'Agent execution failed' } })
        controller.enqueue(`data: ${event}\n\n`)
        controller.close()
      }
    },
  })

  return new Response(stream, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      'Connection': 'keep-alive',
      'X-Accel-Buffering': 'no',
    },
  })
})

// GET /agent/:type/debug
app.get('/:type/debug', async (c) => {
  const agentType = c.req.param('type')
  if (!validAgentTypes.includes(agentType)) return badRequest(c, 'Invalid agent type')
  return success(c, { agent_type: agentType, valid: true })
})

export default app
