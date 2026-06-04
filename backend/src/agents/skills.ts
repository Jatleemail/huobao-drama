import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const SKILLS_DIR = path.resolve(__dirname, '../../../skills')

function stripFrontmatter(content: string): string {
  if (!content.startsWith('---')) return content.trim()
  const end = content.indexOf('\n---', 3)
  if (end === -1) return content.trim()
  return content.slice(end + 4).trim()
}

function readSkill(skillId: string): string {
  const skillPath = path.join(SKILLS_DIR, skillId, 'SKILL.md')
  if (!fs.existsSync(skillPath)) return ''

  const raw = fs.readFileSync(skillPath, 'utf-8')
  const content = stripFrontmatter(raw)
  if (!content) return ''

  return [
    `## Skill: ${skillId}`,
    content,
  ].join('\n')
}

/**
 * Recursively discover all skill IDs for a given agent type.
 * Scans skills/<agentType>/ directory:
 * - Always includes the root SKILL.md if it exists
 * - Recursively includes SKILL.md files in subdirectories
 * - This enables user-created skills (e.g., script_rewriter/custom) to be auto-loaded
 */
function discoverSkills(agentType: string): string[] {
  const baseDir = path.join(SKILLS_DIR, agentType)
  if (!fs.existsSync(baseDir) || !fs.statSync(baseDir).isDirectory()) return []

  const skills: string[] = []

  // Root skill (skills/<agentType>/SKILL.md) — always first if it exists
  if (fs.existsSync(path.join(baseDir, 'SKILL.md'))) {
    skills.push(agentType)
  }

  // Recursively scan subdirectories for additional SKILL.md files
  function scanSubDir(dir: string, prefix: string) {
    const entries = fs.readdirSync(dir, { withFileTypes: true })
    for (const entry of entries) {
      if (!entry.isDirectory()) continue
      const subDir = path.join(dir, entry.name)
      const skillPath = path.join(subDir, 'SKILL.md')
      if (fs.existsSync(skillPath)) {
        const skillId = prefix ? `${prefix}/${entry.name}` : entry.name
        skills.push(skillId)
      }
      // Continue recursing for deeper nesting
      scanSubDir(subDir, prefix ? `${prefix}/${entry.name}` : entry.name)
    }
  }

  scanSubDir(baseDir, agentType)
  return skills
}

export function loadAgentSkills(agentType: string): string {
  const skillIds = discoverSkills(agentType)
  const contents = skillIds
    .map(readSkill)
    .filter(Boolean)

  if (!contents.length) return ''

  return [
    '以下是该 Agent 专属的项目技能规范（SKILL.md）。',
    '不同 Agent 会加载不同 skill；你只需要遵守当前注入的这些技能。',
    '你必须在不违背当前工具边界的前提下优先遵守这些规范；若与用户明确要求冲突，以用户要求为准。',
    '',
    contents.join('\n\n'),
  ].join('\n')
}
