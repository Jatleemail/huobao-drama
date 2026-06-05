<template>
  <div class="settings-layout">
    <aside class="settings-nav">
      <div class="nav-group">
        <div class="nav-group-label">基础</div>
        <button v-for="t in baseTabs" :key="t.id" :class="['nav-item', { active: tab === t.id }]" @click="tab = t.id">
          <component :is="t.icon" :size="14" />
          {{ t.label }}
        </button>
      </div>
      <div class="nav-advanced">
        <label class="advanced-toggle">
          <span>Agent 高级配置</span>
          <input type="checkbox" v-model="showAdvanced" />
          <span class="advanced-slider"></span>
        </label>
        <p class="advanced-note">仅展开 Agent 配置与 Skills。工作台功能和分镜字段保持默认可见。</p>
      </div>
      <div v-if="showAdvanced" class="nav-group">
        <div class="nav-group-label">高级</div>
        <button v-for="t in advancedTabs" :key="t.id" :class="['nav-item', { active: tab === t.id }]"
          @click="tab = t.id">
          <component :is="t.icon" :size="14" />
          {{ t.label }}
        </button>
      </div>
    </aside>

    <div class="settings-content">

      <!-- ===== AI 服务配置 ===== -->
      <div v-if="tab === 'ai'" class="settings-scroll">
        <div class="settings-head">
          <div class="settings-brand">
            <div class="settings-brand-mark">
              <img v-if="showBrandImage" :src="brandLogo" alt="无双漫剧" class="settings-brand-logo"
                @error="showBrandImage = false" />
              <span v-else class="settings-brand-fallback">双</span>
            </div>
            <div class="settings-brand-copy">
              <div class="settings-brand-kicker">Wushuang</div>
              <div class="settings-brand-name">无双漫剧</div>
            </div>
          </div>
          <h2 class="settings-title">AI 服务配置</h2>
          <p class="settings-desc">先用推荐模板快速落配置，再按服务类型微调。工作台创建集时会锁定所选图片、视频和音频能力。</p>
        </div>
        <section class="setup-panel card">
          <div class="setup-panel-head">
            <div>
              <div class="setup-kicker">Quick Setup</div>
              <div class="setup-title">无双推荐配置</div>
              <div class="setup-desc">一键写入文本、图片、视频、音频四类推荐配置，适合作为开箱默认方案。</div>
            </div>
            <button class="btn btn-primary" @click="presetDialog = true">
              <Sparkles :size="14" /> 无双一键配置
            </button>
          </div>
          <div class="preset-grid">
            <article v-for="preset in huobaoPresetCards" :key="preset.serviceType" class="preset-card">
              <div class="preset-card-top">
                <span class="preset-service">{{ preset.label }}</span>
                <span class="tag tag-accent">{{ preset.provider }}</span>
              </div>
              <div class="preset-model mono">{{ preset.model }}</div>
              <div class="preset-base mono">{{ preset.baseUrl }}</div>
            </article>
          </div>
        </section>
        <section class="setup-panel card">
          <div class="setup-panel-head compact">
            <div>
              <div class="setup-title">快捷模板</div>
              <div class="setup-desc">选择服务类型后，直接用模板填充推荐的 `provider / base URL / model`。</div>
            </div>
          </div>
          <div class="template-row">
            <button v-for="st in serviceTypes" :key="st.type" class="template-type-chip" @click="startAddCfg(st.type)">
              {{ st.label }}
            </button>
          </div>
        </section>
        <div class="sections">
          <section v-for="st in serviceTypes" :key="st.type">
            <div class="section-head">
              <div>
                <span class="section-title">{{ st.label }}</span>
                <div class="section-subtitle">{{ serviceMeta[st.type].desc }}</div>
              </div>
              <span v-if="countActive(st.type)" class="tag tag-accent">{{ countActive(st.type) }} 已启用</span>
              <button class="btn btn-ghost btn-sm ml-auto" @click="startAddCfg(st.type)">
                <Plus :size="13" /> 添加
              </button>
            </div>
            <div class="config-list">
              <div v-for="c in byType(st.type)" :key="c.id" class="card config-row">
                <div class="config-info">
                  <div class="config-main">
                    <div class="config-line">
                      <span class="config-provider">{{ c.provider }}</span>
                      <span class="config-name">{{ c.name || `${c.provider}-${c.service_type}` }}</span>
                    </div>
                    <span class="config-model mono truncate">{{ fmtModel(c.model) }}</span>
                    <span class="config-base mono truncate">{{ c.base_url || '未设置 Base URL' }}</span>
                  </div>
                </div>
                <span :class="['tag', c.api_key ? 'tag-success' : 'tag-error']">{{ c.api_key ? '已配置' : '无密钥' }}</span>
                <button class="btn btn-ghost btn-sm" @click="testExistingCfg(c)">测试</button>
                <label class="toggle"><input type="checkbox" :checked="c.is_active"
                    @change="toggleCfg(c)"><span /></label>
                <button class="btn btn-ghost btn-icon" @click="startEditCfg(c)">
                  <Pencil :size="13" />
                </button>
                <button class="btn btn-ghost btn-icon" @click="delCfg(c.id)">
                  <Trash2 :size="13" />
                </button>
              </div>
              <p v-if="!byType(st.type).length" class="config-empty">暂无配置</p>
            </div>
          </section>
        </div>
      </div>

      <!-- ===== Agent 配置 ===== -->
      <div v-else-if="tab === 'agents'" class="settings-scroll">
        <div class="settings-head">
          <div class="settings-brand">
            <div class="settings-brand-mark">
              <img v-if="showBrandImage" :src="brandLogo" alt="Wushang短剧" class="settings-brand-logo"
                @error="showBrandImage = false" />
              <span v-else class="settings-brand-fallback">火</span>
            </div>
            <div class="settings-brand-copy">
              <div class="settings-brand-kicker">Wushang Shorts</div>
              <div class="settings-brand-name">Wushuang短剧</div>
            </div>
          </div>
          <h2 class="settings-title">Agent 配置</h2>
          <p class="settings-desc">高级区只保留 Agent 运行配置。这里可以调整模型、提示词和参数，保存后立即生效。</p>
        </div>
        <div class="agent-list">
          <div v-for="a in agentDefs" :key="a.type" class="card agent-card">
            <div class="agent-card-head" @click="toggleAgentEdit(a.type)">
              <div class="agent-type-badge">{{ a.icon }}</div>
              <div style="flex:1;min-width:0">
                <div style="font-weight:600;font-size:14px">{{ a.label }}</div>
                <div class="dim" style="font-size:12px">{{ a.type }}</div>
              </div>
              <span v-if="getAgentCfg(a.type)" class="tag tag-success">已配置</span>
              <span v-else class="tag">默认</span>
              <ChevronDown :size="14"
                :style="{ transform: editingAgent === a.type ? 'rotate(180deg)' : '', transition: '0.2s' }" />
            </div>
            <div v-if="editingAgent === a.type" class="agent-card-body">
              <label class="field">
                <span class="field-label">模型 <span class="dim">(留空使用 AI 服务默认)</span></span>
                <BaseSelect v-model="agentForm.model" :options="textModelSelectOptions" placeholder="— 使用 AI 服务默认 —"
                  searchable />
              </label>
              <div class="field-row">
                <label class="field">
                  <span class="field-label">Temperature</span>
                  <input v-model.number="agentForm.temperature" class="input" type="number" min="0" max="2"
                    step="0.1" />
                </label>
                <label class="field">
                  <span class="field-label">Max Tokens</span>
                  <input v-model.number="agentForm.max_tokens" class="input" type="number" min="100" max="32000" />
                </label>
              </div>
              <label class="field">
                <span class="field-label">System Prompt</span>
                <textarea v-model="agentForm.system_prompt" class="textarea" rows="12" placeholder="Agent 系统提示词..." />
              </label>
              <div class="agent-card-foot">
                <button class="btn btn-ghost btn-sm" @click="resetAgentPrompt(a.type)">恢复默认</button>
                <span v-if="agentSaved === a.type" class="tag tag-success" style="margin-left:8px">
                  <Check :size="10" /> 已保存
                </span>
                <button class="btn btn-primary btn-sm ml-auto" :disabled="agentSaving" @click="saveAgentCfg(a.type)">
                  <Loader2 v-if="agentSaving" :size="12" class="animate-spin" />
                  保存
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ===== Skills 编辑 ===== -->
      <div v-else-if="tab === 'skills'" class="skills-layout">
        <!-- Agent 左侧列表 -->
        <aside class="skills-agent-list">
          <div class="skills-agent-title">Agent 列表</div>
          <button v-for="a in agentDefs" :key="a.type"
            :class="['skills-agent-item', { active: selectedAgent === a.type }]" @click="selectAgent(a.type)">
            <span class="agent-type-badge">{{ a.icon }}</span>
            <span class="skills-agent-label">{{ a.label }}</span>
            <span v-if="agentSkillCount(a.type) > 0" class="skill-count-badge">{{ agentSkillCount(a.type) }}</span>
          </button>
        </aside>

        <!-- Skill 管理右侧主区域 -->
        <div class="settings-scroll skills-main">
          <div class="settings-head">
            <div class="settings-brand">
              <div class="settings-brand-mark">
                <img v-if="showBrandImage" :src="brandLogo" alt="Wushang短剧" class="settings-brand-logo"
                  @error="showBrandImage = false" />
                <span v-else class="settings-brand-fallback">Wu</span>
              </div>
              <div class="settings-brand-copy">
                <div class="settings-brand-kicker">Wushang Shorts</div>
                <div class="settings-brand-name">Wushang短剧</div>
              </div>
            </div>
            <div style="display:flex;align-items:center;gap:10px">
              <span class="agent-type-badge" style="width:32px;height:32px;font-size:16px">{{ selectedAgentIcon
              }}</span>
              <div>
                <h2 class="settings-title" style="margin:0">{{ selectedAgentLabel }}</h2>
                <div class="dim" style="font-size:12px">{{ selectedAgentType }} — Skills</div>
              </div>
            </div>
            <p class="settings-desc" style="margin-top:10px">Skills 仅作为 Agent 的高级提示词层使用，不影响工作台常规功能入口。</p>
            <button class="btn btn-primary btn-sm" @click="startAddSkill">
              <Plus :size="13" /> 新增 Skill
            </button>
          </div>

          <!-- 无 skill 提示 -->
          <div v-if="!currentSkills.length" class="step-empty" style="padding:48px 24px">
            <div class="empty-visual">
              <FileText :size="28" />
            </div>
            <div class="empty-title">暂无 Skill</div>
            <div class="empty-desc">点击右上角「新增 Skill」创建第一个提示词文件</div>
          </div>

          <!-- Skill 列表 -->
          <div class="skill-list" v-else>
            <div v-for="s in currentSkills" :key="s.id" class="card skill-card">
              <div class="skill-card-head" @click="toggleSkillEdit(s.id)">
                <FileText :size="14" style="color:var(--accent);flex-shrink:0" />
                <div style="flex:1;min-width:0">
                  <div style="font-weight:600;font-size:13px">{{ s.name }}</div>
                  <div class="dim" style="font-size:11px">{{ s.description }}</div>
                </div>
                <button class="btn btn-ghost btn-icon" style="margin-right:4px" @click.stop="deleteSkill(s.id)">
                  <Trash2 :size="13" />
                </button>
                <ChevronDown :size="14"
                  :style="{ transform: editingSkill === s.id ? 'rotate(180deg)' : '', transition: '0.2s' }" />
              </div>
              <div v-if="editingSkill === s.id" class="skill-card-body">
                <textarea v-model="skillContent" class="textarea mono" rows="20" style="font-size:12px;line-height:1.6"
                  placeholder="编写 SKILL.md 内容..." />
                <div class="skill-card-foot">
                  <span class="dim" style="font-size:11px">skills/{{ selectedAgentType }}/{{ s.id }}/SKILL.md</span>
                  <span v-if="skillSaved === s.id" class="tag tag-success" style="margin-left:8px">
                    <Check :size="10" /> 已保存
                  </span>
                  <button class="btn btn-primary btn-sm ml-auto" :disabled="skillSaving" @click="saveSkill(s.id)">
                    <Loader2 v-if="skillSaving" :size="12" class="animate-spin" />
                    保存
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- AI Config Dialog -->
    <div v-if="cfgDialog" class="overlay" @click.self="cfgDialog = false">
      <form class="modal card config-modal" @submit.prevent="saveCfg">
        <div class="config-modal-head">
          <div>
            <div class="setup-kicker">{{ cfgEditId ? 'Edit Config' : 'New Config' }}</div>
            <h2 class="modal-title">{{ cfgEditId ? '编辑服务配置' : `添加${serviceMeta[cfgForm.service_type].label}服务` }}</h2>
            <div class="modal-note">推荐先选择模板，系统会自动填入更合理的 `Base URL` 与默认模型。</div>
          </div>
          <span class="tag tag-accent">{{ serviceMeta[cfgForm.service_type].label }}</span>
        </div>
        <div class="preset-picker">
          <button v-for="preset in presetsByType(cfgForm.service_type)"
            :key="`${cfgForm.service_type}-${preset.provider}`" type="button" class="preset-pill"
            @click="applyProviderPreset(cfgForm.service_type, preset.provider)">
            {{ preset.label }}
          </button>
        </div>
        <label class="field">
          <span class="field-label">配置名称</span>
          <input v-model="cfgForm.name" class="input" placeholder="无双默认图像服务" />
        </label>
        <label class="field"><span class="field-label">服务商</span>
          <BaseSelect v-model="cfgForm.provider" :options="providerSelectOptions" placeholder="选择服务商" searchable />
        </label>
        <label class="field">
          <span class="field-label">优先级</span>
          <input v-model.number="cfgForm.priority" class="input" type="number" min="0" max="999" />
          <span class="field-hint">数值越高越优先。工作台默认会优先使用同类型里优先级最高的启用配置。</span>
        </label>
        <label class="field"><span class="field-label">API Key</span><input v-model="cfgForm.api_key" class="input"
            type="password" placeholder="sk-..." /></label>
        <label class="field"><span class="field-label">Base URL</span><input v-model="cfgForm.base_url" class="input"
            placeholder="https://..." /></label>
        <div class="endpoint-hint">
          <span class="dim">实际端点前缀：</span>
          <span class="mono">{{ endpointHint }}</span>
        </div>
        <label class="field"><span class="field-label">模型（逗号分隔）</span><input v-model="cfgForm.modelStr" class="input"
            placeholder="model-name" /></label>
        <div v-if="cfgTestResult" class="test-result"
          :class="{ ok: cfgTestResult.reachable, bad: !cfgTestResult.reachable }">
          <div class="test-result-head">
            <span class="tag" :class="cfgTestResult.reachable ? 'tag-success' : 'tag-error'">{{ cfgTestResult.status ||
              'ERROR' }}</span>
            <span>{{ cfgTestResult.message }}</span>
          </div>
          <div class="mono test-result-url">{{ cfgTestResult.method }} {{ cfgTestResult.url }}</div>
          <div v-if="cfgTestResult.response_preview" class="mono test-result-preview">{{ cfgTestResult.response_preview
          }}</div>
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-ghost" :disabled="cfgTesting" @click="testDraftCfg">
            <Loader2 v-if="cfgTesting" :size="12" class="animate-spin" />
            <span v-else>测试配置</span>
          </button>
          <button type="button" class="btn" @click="cfgDialog = false">取消</button>
          <button type="submit" class="btn btn-primary">保存</button>
        </div>
      </form>
    </div>

    <!-- Huobao Preset Dialog -->
    <div v-if="presetDialog" class="overlay" @click.self="presetDialog = false">
      <form class="modal card config-modal" @submit.prevent="applyHuobaoPreset">
        <div class="config-modal-head">
          <div>
            <div class="setup-kicker">Wushuang Preset</div>
            <h2 class="modal-title">一键配置</h2>
            <div class="modal-note">按无双推荐链路自动创建或更新 4 条服务配置，并同时初始化 5 个 Agent 的默认模型。</div>
          </div>
          <span class="tag tag-success">推荐</span>
        </div>
        <div class="huobao-grid">
          <label class="field">
            <span class="field-label">wushuang API Key <span class="dim">(统一用于文本 / 图片 / 视频 / 音频)</span></span>
            <input v-model="huobaoForm.apiKey" class="input" type="password" placeholder="用于 api.chatfire.site 全链路服务" />
            <span class="field-hint">还没有账号？<a href="https://api.chatfire.site/" target="_blank" rel="noopener">立即注册
                →</a></span>
          </label>
        </div>
        <div class="preset-grid compact">
          <article v-for="preset in huobaoPresetCards" :key="`${preset.serviceType}-${preset.provider}`"
            class="preset-card">
            <div class="preset-card-top">
              <span class="preset-service">{{ preset.label }}</span>
              <span class="tag tag-accent">{{ preset.provider }}</span>
            </div>
            <div class="preset-model mono">{{ preset.model }}</div>
            <div class="preset-base mono">{{ preset.baseUrl }}</div>
          </article>
        </div>
        <div class="modal-actions">
          <button type="button" class="btn" @click="presetDialog = false">取消</button>
          <button type="submit" class="btn btn-primary">创建并启用</button>
        </div>
      </form>
    </div>

    <!-- Add Skill Dialog -->
    <div v-if="addSkillDialog" class="overlay" @click.self="addSkillDialog = false">
      <form class="modal card" @submit.prevent="confirmAddSkill">
        <h2 class="modal-title">新增 Skill — {{ selectedAgentLabel }}</h2>
        <label class="field">
          <span class="field-label">Skill 目录名 <span class="dim">(英文，唯一)</span></span>
          <input v-model="newSkillForm.id" class="input" placeholder="如 custom-extraction" />
        </label>
        <label class="field">
          <span class="field-label">名称</span>
          <input v-model="newSkillForm.name" class="input" placeholder="如 自定义提取规则" />
        </label>
        <label class="field">
          <span class="field-label">描述</span>
          <input v-model="newSkillForm.description" class="input" placeholder="简短描述此 Skill 的用途" />
        </label>
        <div class="modal-actions">
          <button type="button" class="btn" @click="addSkillDialog = false">取消</button>
          <button type="submit" class="btn btn-primary" :disabled="!newSkillForm.id">创建</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { Plus, Pencil, Trash2, FileText, ChevronDown, Check, Loader2, Bot, Cpu, Sparkles } from 'lucide-vue-next'
import BaseSelect from '~/components/BaseSelect.vue'
import { toast } from 'vue-sonner'
import { aiConfigAPI, agentConfigAPI, skillsAPI } from '~/composables/useApi'
import brandLogo from '~/assets/huobao-logo.png'

const showBrandImage = ref(true)
const tab = ref('ai')
const showAdvanced = ref(false)
const baseTabs = [
  { id: 'ai', label: 'AI 服务', icon: Cpu },
]
const advancedTabs = [
  { id: 'agents', label: 'Agent 配置', icon: Bot },
  { id: 'skills', label: 'Skills', icon: FileText },
]
watch(showAdvanced, (v) => {
  if (!v && tab.value !== 'ai') tab.value = 'ai'
})

// ===== AI Service Configs =====
const cfgs = ref([])
const cfgDialog = ref(false)
const cfgEditId = ref(null)
const presetDialog = ref(false)
const cfgTesting = ref(false)
const cfgTestResult = ref(null)
const cfgForm = reactive({ name: '', provider: '', api_key: '', base_url: '', modelStr: '', service_type: 'text', priority: 0 })
const huobaoForm = reactive({ apiKey: '' })
const serviceTypes = [{ type: 'text', label: '文本' }, { type: 'image', label: '图片' }, { type: 'video', label: '视频' }, { type: 'audio', label: '音频' }]
const providers = ['ali', 'chatfire', 'gemini', 'minimax', 'openai', 'openrouter', 'vidu', 'volcengine']
const providerSelectOptions = computed(() => providers.map(p => ({ label: p, value: p })))
const serviceMeta = {
  text: { label: '文本', desc: '剧本改写、角色场景提取、分镜拆解等 Agent 文本能力' },
  image: { label: '图片', desc: '角色图、场景图、镜头图与首尾帧等静态图像生成' },
  video: { label: '视频', desc: '镜头视频生成，支持单图、多图和首尾帧模式' },
  audio: { label: '音频', desc: '角色试听、旁白与对白语音生成' },
}
const providerPresets = {
  text: {
    chatfire: { label: 'ChatFire 推荐', baseUrl: 'https://api.chatfire.site', models: ['gemini-3-pro-preview'] },
    openrouter: { label: 'OpenRouter 推荐', baseUrl: 'https://openrouter.ai/api', models: ['google/gemini-3-flash-preview'] },
    openai: { label: 'OpenAI 推荐', baseUrl: 'https://api.openai.com', models: ['gpt-4.1-mini'] },
  },
  image: {
    chatfire: { label: 'ChatFire 推荐', baseUrl: 'https://api.chatfire.site', models: ['doubao-seedream-4-5-251128'] },
    gemini: { label: 'Gemini 推荐', baseUrl: 'https://api.chatfire.site', models: ['gemini-3-pro-image-preview'] },
    volcengine: { label: '火山推荐', baseUrl: 'https://ark.cn-beijing.volces.com', models: ['doubao-seedream-4-0-250828'] },
  },
  video: {
    volcengine: { label: '火宝视频', baseUrl: 'https://api.chatfire.site/volcengine', models: ['doubao-seedance-1-5-pro-251215'] },
    vidu: { label: 'Vidu 推荐', baseUrl: 'https://api.vidu.com', models: ['viduq3-turbo'] },
    ali: { label: '阿里推荐', baseUrl: 'https://dashscope.aliyuncs.com', models: ['wan2.6-i2v-flash'] },
  },
  audio: {
    minimax: { label: '火宝音频', baseUrl: 'https://api.chatfire.site/minimax', models: ['speech-2.8-hd'] },
  },
}
const huobaoPresetCards = [
  { serviceType: 'text', label: '文本', provider: 'chatfire', baseUrl: 'https://api.chatfire.site', model: 'gemini-3-pro-preview', priority: 100 },
  { serviceType: 'image', label: '图片', provider: 'gemini', baseUrl: 'https://api.chatfire.site', model: 'gemini-3-pro-image-preview', priority: 99 },
  { serviceType: 'video', label: '视频', provider: 'volcengine', baseUrl: 'https://api.chatfire.site/volcengine', model: 'doubao-seedance-1-5-pro-251215', priority: 98 },
  { serviceType: 'audio', label: '音频', provider: 'minimax', baseUrl: 'https://api.chatfire.site/minimax', model: 'speech-2.8-hd', priority: 97 },
]
const endpointPrefixes = {
  chatfire: '/v1',
  openai: '/v1',
  openrouter: '/v1',
  minimax: '/v1',
  gemini: '/v1beta',
  volcengine: '/api/v3',
  ali: '/api/v1',
  vidu: '/ent/v2',
}

const endpointHint = computed(() => {
  const provider = cfgForm.provider
  const base = cfgForm.base_url || 'https://...'
  const prefix = endpointPrefixes[provider] || ''
  if (!provider) return '选择服务商后显示推荐端点前缀'
  return `${base}${prefix}`
})

function byType(t) { return cfgs.value.filter(c => c.service_type === t) }
function countActive(t) { return byType(t).filter(c => c.is_active).length }
function fmtModel(m) { return Array.isArray(m) ? m.join(', ') : m || '—' }
function presetsByType(type) {
  const group = providerPresets[type] || {}
  return Object.entries(group).map(([provider, preset]) => ({ provider, ...preset }))
}
function applyProviderPreset(type, provider) {
  const preset = providerPresets[type]?.[provider]
  if (!preset) return
  cfgForm.provider = provider
  cfgForm.base_url = preset.baseUrl
  cfgForm.modelStr = preset.models.join(', ')
  cfgForm.name = `${preset.label}-${serviceMeta[type].label}`
}

async function loadCfgs() { try { cfgs.value = await aiConfigAPI.list() } catch (e) { toast.error(e.message) } }
async function toggleCfg(c) { await aiConfigAPI.update(c.id, { is_active: !c.is_active }); loadCfgs() }
async function delCfg(id) { await aiConfigAPI.del(id); toast.success('已删除'); loadCfgs() }
function startAddCfg(t) {
  cfgEditId.value = null
  cfgTestResult.value = null
  Object.assign(cfgForm, { name: '', provider: '', api_key: '', base_url: '', modelStr: '', service_type: t, priority: 0 })
  const firstPreset = presetsByType(t)[0]
  if (firstPreset) applyProviderPreset(t, firstPreset.provider)
  cfgDialog.value = true
}
function startEditCfg(c) {
  cfgEditId.value = c.id
  cfgTestResult.value = null
  Object.assign(cfgForm, {
    name: c.name || '',
    provider: c.provider,
    api_key: c.api_key || '',
    base_url: c.base_url || '',
    modelStr: fmtModel(c.model),
    service_type: c.service_type,
    priority: c.priority ?? 0,
  })
  cfgDialog.value = true
}
async function testCfgPayload(payload) {
  cfgTesting.value = true
  try {
    cfgTestResult.value = await aiConfigAPI.test(payload)
    if (cfgTestResult.value.reachable) toast.success('端点已响应')
    else toast.warning('端点未通过测试')
  } catch (e) {
    toast.error(e.message)
  } finally {
    cfgTesting.value = false
  }
}
async function testDraftCfg() {
  await testCfgPayload({
    service_type: cfgForm.service_type,
    provider: cfgForm.provider,
    api_key: cfgForm.api_key,
    base_url: cfgForm.base_url,
    model: cfgForm.modelStr.split(',').map(s => s.trim()).filter(Boolean),
  })
}
async function testExistingCfg(c) {
  startEditCfg(c)
  await testCfgPayload({
    service_type: c.service_type,
    provider: c.provider,
    api_key: c.api_key || '',
    base_url: c.base_url || '',
    model: Array.isArray(c.model) ? c.model : [],
  })
}
async function saveCfg() {
  if (!cfgForm.provider) { toast.warning('选择服务商'); return }
  const models = cfgForm.modelStr.split(',').map(s => s.trim()).filter(Boolean)
  try {
    if (cfgEditId.value) await aiConfigAPI.update(cfgEditId.value, { name: cfgForm.name, provider: cfgForm.provider, api_key: cfgForm.api_key, base_url: cfgForm.base_url, model: models, priority: cfgForm.priority })
    else await aiConfigAPI.create({ service_type: cfgForm.service_type, provider: cfgForm.provider, name: cfgForm.name || `${cfgForm.provider}-${cfgForm.service_type}`, api_key: cfgForm.api_key, base_url: cfgForm.base_url, model: models, priority: cfgForm.priority })
    cfgDialog.value = false; toast.success('已保存'); loadCfgs()
  } catch (e) { toast.error(e.message) }
}
async function applyHuobaoPreset() {
  if (!huobaoForm.apiKey) {
    toast.warning('请填写 Huobao API Key')
    return
  }
  try {
    await aiConfigAPI.huobaoPreset(huobaoForm.apiKey)
    await loadCfgs()
    await loadAgents()
    presetDialog.value = false
    toast.success('推荐配置与默认 Agent LLM 已写入')
  } catch (e) {
    toast.error(e.message)
  }
}

// ===== Agent Configs =====
const agentCfgs = ref([])
const editingAgent = ref(null)
const agentSaving = ref(false)
const agentSaved = ref(null)
const agentForm = reactive({ model: '', temperature: 0.7, max_tokens: 4096, system_prompt: '' })

const agentDefs = [
  { type: 'script_rewriter', label: '剧本改写', icon: '📝' },
  { type: 'extractor', label: '角色场景提取', icon: '🔍' },
  { type: 'storyboard_breaker', label: '分镜拆解', icon: '🎬' },
  { type: 'voice_assigner', label: '音色分配', icon: '🎙' },
  { type: 'grid_prompt_generator', label: '图片提示词生成', icon: '🖼' },
]

// 与后端 DEFAULT_PROMPTS 保持同步
const defaultPrompts = {
  script_rewriter: `你是资深短剧编剧，专门将小说章节改编为适合短视频平台的格式化剧本。

## 核心原则
- 保留主线剧情和角色关系，不改变核心冲突
- 将叙述性/心理描写转化为可视化动作和对话
- 每场戏控制在 30-60 秒，含明确情绪转折
- **不写镜头语言**：景别、角度、运镜属于分镜拆解步骤，不要在剧本改写中涉及

## 工作流程
1. 调用 read_episode_script 读取原始内容
2. 根据读取到的内容进行改写（输出格式化剧本格式）
3. 调用 save_script 保存改写后的完整剧本

## 输出格式（严格遵循）
- 场景头：## S01 | 内景/外景 · 地点 | 具体时段（如黄昏、深夜、清晨）
- 动作描写：自然段落，不含任何镜头术语
- 对白：角色名：（表情/状态）台词内容
- 场景编号从 S01 开始连续递增

## 质量要求
- 对白占比 ≥ 60%，避免大段纯叙述
- 每场戏至少有一个情绪锚点（转折/张力/悬疑/冲突）
- 保留原作关键对话，凝练修辞性描述
- 心理活动转化为：表情动作 / 画外音 / 环境映射
- 角色语言风格保持一致（市井角色用口语，文人角色用书面语）

## 异常处理
- 若原文缺少对白，根据角色性格合理补充
- 若原文段落过长，拆分为多个短场景
- 若内容不适合影视化（纯说明/元叙述），标注原因并跳过`,
  extractor: `你是制片助理 AI，从格式化剧本中提取角色与场景信息，并与项目已有数据进行智能合并。

## 提取范围
- 只提取**当前集**真实出现或被明确提及的角色和场景
- 角色优先级：有台词 > 有重要动作 > 被提及影响剧情 > 龙套
- 场景优先级：主要事件发生地 > 过渡/提及场景

## 工作流程
1. 调用 read_script_for_extraction 读取当前集格式化剧本
2. 调用 read_existing_characters 读取项目已有角色和当前集已关联角色
3. 调用 read_existing_scenes 读取项目已有场景和当前集已关联场景
4. 分析本集实际出现的角色，逐个判断新增或合并
5. 调用 save_dedup_characters 保存角色（自动去重合并并关联当前集）
6. 分析本集涉及的所有场景，逐个判断新增或复用
7. 调用 save_dedup_scenes 保存场景（自动去重复用并关联当前集）

## 角色提取与去重合并
每个角色包含：姓名、性别、年龄层、角色定位（主角/配角/龙套）、外貌描写（体型+面部+发型+服装+标志性特征，≥100字）、性格标签（3-5个关键词）、角色简述（背景+与主线关系）

### 去重合并规则
- 按姓名精确匹配现有角色
- 同名角色：**合并信息**而非简单保留现有
  - 若新提取的外貌/性格描述更详细 → 更新对应字段
  - 若新旧信息冲突 → 优先采用当前集描述，并标注差异
  - 角色定位升级（如龙套→配角）→ 取高级别
- 新角色：正常新增并关联到当前集

## 场景提取与去重复用
每个场景包含：地点名称、时间段+光线、氛围描述（色调/情绪/声音）、英文图片提示词（纯背景，无人物）

### 去重复用规则
- 按「地点 + 时间段」精确匹配
- 同地点同时间段 → 复用已有场景，不创建重复；若新描述更丰富则更新
- 同地点不同时间段 → 视为新场景（光线/氛围不同）
- 复用场景也必须关联到当前集

## 道具提取
从剧本中提取反复出现或对剧情起关键作用的道具/物品。

### 提取触发条件（满足任一即提取）
1. 被角色持有/使用且有特写描述
2. 推动剧情发展的关键物品（信物、武器、钥匙、书信等）
3. 反复出现（≥2次）的环境物件或标志性物品

### 每个道具包含
- name：道具名称（简洁准确，2-6字）
- category：类型分类（武器/文件书信/食物饮品/交通工具/装饰品/科技设备/自然物品/其他）
- description：外观描述（材质、颜色、形状、大小、标志性细节，≥50字）
- prompt：英文图片提示词（纯物品产品图风格，clean product shot, no people, no hands, isolated on neutral background）

### 去重合并规则
- 按名称精确匹配现有道具
- 同名道具：合并信息，取更详细描述和提示词
- 新类型覆盖旧类型（如从「其他」升级为「武器」）
- 新道具：正常新增

### 工作流程中的道具处理
在角色和场景提取完成后：
8. 调用 read_existing_props 读取项目已有道具
9. 分析本集涉及的道具，逐个判断新增或去重
10. 调用 save_dedup_props 保存道具（自动去重合并）

## 质量自检
- □ 所有有台词的角色都已提取
- □ 外貌描述 ≥ 100 字，性格标签 ≥ 3 个
- □ 场景含光线+氛围信息
- □ 所有关键道具都已提取
- □ 没有因去重而丢失新信息
- □ 提取的角色、场景、道具已关联到当前集`,
  storyboard_breaker: `你是资深影视分镜师，将格式化剧本拆解为可执行的完整分镜方案。

## 拆解原则
- 每个镜头聚焦单一动作或情绪节拍，时长 10-15 秒
- 镜头序列需保持叙事连续性，相邻镜头有视觉/动作衔接
- 整集总时长合理（镜头数 ≈ 剧本场景数 × 2~4）
- 空镜头（无角色）占比 ≤ 20%，用于过渡和氛围建立

## 工作流程
1. 调用 read_storyboard_context 读取剧本、角色列表、场景列表、已有分镜摘要
2. 将剧本拆解为镜头序列，确保剧情完整连续
3. 为每个镜头补全全部 17 个字段（见下方），而不是只填 video_prompt
4. 调用 save_storyboards 一次性保存完整分镜

## 必填字段（共 17 个，每个镜头尽可能完整）

### 基础信息
- title：3-8 字镜头标题（如"深夜惊醒"）
- shot_type：景别（远景/全景/中景/近景/特写）
- angle：机位角度（平视/仰视/俯视/侧拍/背面）
- movement：运镜（固定/推镜/拉镜/摇镜/跟拍/移镜）
- duration：10-15 秒

### 内容信息
- location：地点名（优先复用 scenes 中已有地点）
- time：时间段（优先复用 scenes 中已有时间）
- character_ids：角色 ID 数组（从 characters 列表中选择；空镜头可为空数组）
- action：角色动作与肢体表演细节
- dialogue：该镜头对白/旁白（无对白可空；旁白格式："旁白：内容"）
- description：镜头概述（供人阅读，说明画面内容和叙事功能）
- result：镜头结束时的画面结果或状态变化
- atmosphere：光线+色调+环境感受

### AI 生成用提示词
- image_prompt：**静态**画面提示词（突出单帧构图、角色外观、环境、光线）
- video_prompt：**动态**提示词，按 3 秒分段（格式见下方）
- bgm_prompt：配乐风格短语（如"悬疑钢琴低音"，不能空泛到只写"紧张"）
- sound_effect：关键音效短语（如"门铃叮当+脚步声"）

### 关联
- scene_id：若可匹配已有场景，必须填写正确 scene_id
- prop_ids：道具 ID 数组（从道具列表中选择；镜头中明显出现或被使用的道具必须绑定；无可为空数组）

## video_prompt 格式规范（必填）
按 3 秒为一段，用时间标记和标签分隔：

0-3秒：<location>咖啡厅</location>，近景，<role>小明</role>低头看手机，表情焦虑。
<n>3-6秒：<location>咖啡厅</location>，全景，门铃响，<role>小红</role>推门走入。
<n>6-9秒：<location>咖啡厅</location>，中景，<role>小红</role>微笑走向小明，坐下。

标签说明：
- <location>地点</location> — 场景标记
- <role>角色名</role> — 角色标记
- <voice>角色名</voice> — 画外音/旁白标记
- <n> — 时间段分隔符

## 场景关联规则
- 优先复用已有 scene_id，不凭空创造新场景
- location+time 可明确匹配已有场景时，必须回填正确 scene_id
- 若剧本内容在已有场景中，不要重复创造新场景描述

## 角色绑定规则
- character_ids 必须从 characters 列表中选择
- 镜头中出现/说话/发生动作的角色必须绑定
- 纯环境、物件、过渡空镜头可传空数组
- 不要绑定未出现的角色

## 道具绑定规则
- prop_ids 必须从道具列表中选择
- 镜头中明显出现或被角色持有/使用的道具必须绑定
- 不凭空编造道具，不绑定未在道具列表中出现的道具 ID
- 无道具镜头可传空数组

## image_prompt 与 video_prompt 区分
- image_prompt：单帧静态画面，侧重构图、角色外观、环境细节、光线
- video_prompt：时间推进的动态描述，侧重动作变化、镜头运动、时序
- 两者内容不应雷同

## 已有分镜处理
- 若存在 existing_storyboards，仅在用户明确要求增量修改时参考
- 默认按当前剧本重新完整生成并保存整集分镜

## 质量自检
- □ 每个镜头 17 字段基本完整
- □ video_prompt 使用 <location>/<role> 标签
- □ character_ids 来自已有角色列表
- □ scene_id 优先复用已有场景
- □ image_prompt 和 video_prompt 内容有明确区分`,
  voice_assigner: `你是配音导演 AI，为项目中每个角色从可用音色库中选出最佳匹配。

## 工作流程
1. 调用 list_voices 获取可用音色列表
2. 调用 get_characters 获取所有角色信息
3. 按匹配维度分析每个角色，选择最佳音色
4. 对每个角色调用 assign_voice 分配音色，并给出具体选择理由

## 匹配维度（按优先级排序）
1. **性别匹配**：男声/女声/中性声
2. **年龄匹配**：少年(<18) / 青年(18-35) / 中年(35-55) / 老年(55+)
3. **性格匹配**：
   - 活泼开朗 → 明亮有活力、语速偏快
   - 沉稳内敛 → 低沉稳重、语速中等
   - 温柔体贴 → 柔和甜美、气息充足
   - 威严霸气 → 浑厚有力、共鸣强
   - 阴险狡猾 → 尖锐或低沉带沙哑
   - 天真单纯 → 清脆明亮、童声特质
4. **角色定位匹配**：
   - 主角 → 辨识度高、有特色的音色
   - 重要配角 → 与主角音色形成对比
   - 龙套 → 中性通用音色

## 音色冲突处理
- 若多个角色最适合同一音色：
  1. 主角优先获得
  2. 其余角色选择次优匹配（音色相近但不同）
  3. 在分配理由中标注冲突和替代决策
- 同一场景中有对话互动的角色，音色应有足够区分度

## 输出要求
为每个角色调用 assign_voice 时，选择理由应具体：
- 包含角色核心特征（性别/年龄/性格）
- 说明匹配了哪个维度（如"青年男声+沉稳性格匹配"）
- 避免泛化表述（不说"音色合适"，要说具体为什么合适）

## 质量要求
- 每个角色都必须分配，不遗漏
- 不存在未分配音色的角色`,
  grid_prompt_generator: `你是专业的 AI 图像提示词工程师，为角色、场景和宫格图生成高质量英文提示词。
参考文档：skills/grid_prompt_generator/reference/ 下的模板。

## 通用规范
- 所有提示词使用英文
- 必须包含风格一致性约束：consistent art style
- 必须包含质量标签：high quality, detailed
- 禁止内容：text, watermark, signature, logo, lettering, UI elements
- 根据项目视觉风格动态替换 cinematic 为对应风格标签

你将收到用户的请求，告知要生成哪种类型的提示词：
- "角色" → 生成角色图片提示词
- "场景" → 生成场景图片提示词
- "宫格" → 生成宫格图提示词

---

## 一、角色图片提示词

### 工作流程
1. 调用 read_characters 读取所有角色信息（含 appearance/personality/role）
2. 逐个生成英文提示词

### 提示词结构（按此顺序）
[性别+年龄层] character, [姓名], [体型描述], [面部特征].
[Hair details — 长度/颜色/发型].
[Clothing/Accessories — 服装风格/颜色/配饰].
[Pose and expression — 姿态/表情应与 personality 呼应].
Background: [simple/gradient/neutral], no distractions.
Style: [项目视觉风格], high quality, detailed, character concept art, consistent art style, no text, no watermark.

### 质量要求
- 外貌描写基于 appearance 字段，不凭空编造
- personality 体现在表情和姿态中
- 角色间风格保持一致

---

## 二、场景图片提示词

### 工作流程
1. 调用 read_scenes 读取所有场景信息（含 location/time/prompt）
2. 逐个生成英文提示词

### 提示词结构
A [项目视觉风格] pure background scene of [location] at [time period].
The scene shows [具体环境细节：建筑/物件/光线/氛围].
No characters, no people, no figures, no silhouettes.
Style: [项目视觉风格], rich details, atmospheric lighting, high quality, consistent art style, no text, no watermark.
Mood: [2-3 words mood description].

### 质量要求
- 明确声明 no characters / no people — 这是纯背景环境图
- 氛围词具体（如 peaceful, nostalgic，不说"good atmosphere"）

---

## 三、宫格图提示词

### 工作流程
1. 调用 read_shots_for_grid 读取选中镜头的详细信息
2. 根据 mode 调用 generate_grid_prompt
3. 若用户消息中包含"参考图映射：图片1=...；图片2=..."，将映射文本原样作为 reference_legend 传入

### 三种模式

#### first_frame（首帧宫格）
每个格子 = 一个镜头的起始画面。严格按用户指定的 rows×cols 生成。
[rows x cols] grid, exactly [N] visible panels, consistent art style, [项目视觉风格],
格1: [shot 1 opening scene — 1-2句英文描述],
格2: [shot 2 opening scene — 1-2句英文描述],
...
格N: [shot N opening scene],
high quality, cinematic lighting, no merged panels, no missing panels, no text, no watermark

#### first_last（首尾帧宫格）
每个镜头占 2 格（首帧+尾帧），营造节奏对比。严格按用户指定的 rows×cols 生成。
[rows x cols] grid, exactly [N] visible panels, consistent art style, [项目视觉风格],
格1: [opening beat — 1-2句英文],
格2: [closing beat — 1-2句英文，与格1形成对比],
格3: [opening beat],
格4: [closing beat],
...
high quality, cinematic, continuous motion implied, no merged panels, no missing panels, no text

#### multi_ref（多参考宫格）
所有格子 = 同一镜头的不同角度/构图。严格按用户指定的 rows×cols 生成。
[rows x cols] grid, exactly [N] visible panels, same scene different angles, [项目视觉风格],
[shared scene description — 1句总述],
格1: wide shot establishing — [细节],
格2: medium shot character focus — [细节],
格3: close-up detail — [细节],
格4: dramatic angle — [细节],
...
consistent lighting and color palette, no merged panels, no missing panels, no text

### 宫格通用规则
- 必须写 exactly N visible panels（N = rows × cols）
- 必须写 no merged panels, no missing panels
- 格子位置用"格1/格2/..."，参考图用"图片1/图片2/..."，不可混用
- 尺寸建议：每格 960×540，总图 = (960×cols) × (540×rows)`,
}

function getAgentCfg(type) {
  return agentCfgs.value.find(a => a.agent_type === type)
}

const textModelGroups = computed(() => {
  return cfgs.value
    .filter(c => c.service_type === 'text' && c.is_active && c.api_key)
    .map(c => ({
      label: `${c.provider} — ${c.name}`,
      models: Array.isArray(c.model) ? c.model : (c.model ? [c.model] : []),
    }))
    .filter(g => g.models.length > 0)
})

const textModelSelectOptions = computed(() =>
  textModelGroups.value.map(g => ({
    label: g.label,
    options: g.models.map(m => ({ label: m, value: m })),
  }))
)

async function loadAgents() {
  try { agentCfgs.value = await agentConfigAPI.list() }
  catch (e) { toast.error(e.message) }
}

function toggleAgentEdit(type) {
  if (editingAgent.value === type) { editingAgent.value = null; return }
  const cfg = getAgentCfg(type)
  agentForm.model = cfg?.model || ''
  agentForm.temperature = cfg?.temperature ?? 0.7
  agentForm.max_tokens = cfg?.max_tokens ?? 4096
  agentForm.system_prompt = cfg?.system_prompt || defaultPrompts[type] || ''
  agentSaved.value = null
  editingAgent.value = type
}

function resetAgentPrompt(type) {
  agentForm.system_prompt = defaultPrompts[type] || ''
  toast.info('已恢复默认提示词，点击保存生效')
}

async function saveAgentCfg(type) {
  agentSaving.value = true
  agentSaved.value = null
  try {
    const existing = getAgentCfg(type)
    const data = {
      agent_type: type,
      name: agentDefs.find(a => a.type === type)?.label || type,
      model: agentForm.model,
      temperature: agentForm.temperature,
      max_tokens: agentForm.max_tokens,
      system_prompt: agentForm.system_prompt,
    }
    if (existing) {
      await agentConfigAPI.update(existing.id, data)
    } else {
      await agentConfigAPI.create(data)
    }
    await loadAgents()
    agentSaved.value = type
    toast.success(`${agentDefs.find(a => a.type === type)?.label} 配置已保存`)
    setTimeout(() => { if (agentSaved.value === type) agentSaved.value = null }, 3000)
  } catch (e) {
    toast.error(e.message)
  } finally {
    agentSaving.value = false
  }
}

// ===== Skills =====
const selectedAgent = ref('script_rewriter')
const allSkills = ref([])   // { id, name, description }[]
const editingSkill = ref(null)
const skillContent = ref('')
const skillSaving = ref(false)
const skillSaved = ref(null)
const addSkillDialog = ref(false)
const newSkillForm = reactive({ id: '', name: '', description: '' })

const selectedAgentType = computed(() => selectedAgent.value)
const selectedAgentLabel = computed(() => agentDefs.find(a => a.type === selectedAgent.value)?.label || '')
const selectedAgentIcon = computed(() => agentDefs.find(a => a.type === selectedAgent.value)?.icon || '')

function agentSkillCount(type) {
  return allSkills.value.filter(s => s.id === type || s.id.startsWith(type + '/')).length
}

const currentSkills = computed(() =>
  allSkills.value.filter(s => s.id === selectedAgent.value || s.id.startsWith(selectedAgent.value + '/'))
)

async function loadAllSkills() {
  try { allSkills.value = await skillsAPI.list() }
  catch (e) { toast.error(e.message) }
}

async function selectAgent(type) {
  selectedAgent.value = type
  editingSkill.value = null
}

function startAddSkill() {
  newSkillForm.id = ''
  newSkillForm.name = ''
  newSkillForm.description = ''
  addSkillDialog.value = true
}

async function confirmAddSkill() {
  if (!newSkillForm.id) return
  const skillId = `${selectedAgent.value}/${newSkillForm.id}`
  try {
    await skillsAPI.create({ id: skillId, name: newSkillForm.name, description: newSkillForm.description })
    addSkillDialog.value = false
    await loadAllSkills()
    toast.success('Skill 创建成功')
  } catch (e) {
    toast.error(e.message)
  }
}

async function deleteSkill(id) {
  if (!confirm(`确定删除 Skill「${id}」？`)) return
  try {
    await skillsAPI.del(id)
    if (editingSkill.value === id) editingSkill.value = null
    await loadAllSkills()
    toast.success('已删除')
  } catch (e) {
    toast.error(e.message)
  }
}

async function toggleSkillEdit(id) {
  if (editingSkill.value === id) { editingSkill.value = null; return }
  try {
    const res = await skillsAPI.get(id)
    skillContent.value = res.content
    skillSaved.value = null
    editingSkill.value = id
  } catch (e) { toast.error(e.message) }
}

async function saveSkill(id) {
  skillSaving.value = true
  skillSaved.value = null
  try {
    await skillsAPI.update(id, skillContent.value)
    await loadAllSkills()
    skillSaved.value = id
    toast.success(`已保存`)
    setTimeout(() => { if (skillSaved.value === id) skillSaved.value = null }, 3000)
  } catch (e) {
    toast.error(e.message)
  } finally {
    skillSaving.value = false
  }
}

onMounted(() => { loadCfgs(); loadAgents(); loadAllSkills() })
</script>

<style scoped>
.settings-layout {
  display: flex;
  height: 100%;
  background: var(--bg-base);
}

.settings-nav {
  width: 220px;
  flex-shrink: 0;
  padding: 16px 10px;
  border-right: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  gap: 14px;
  background: var(--bg-1);
}

.nav-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nav-group-label {
  font-size: 10px;
  font-weight: 700;
  color: var(--text-3);
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 0 10px 4px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 12px;
  font-size: 13px;
  border: none;
  background: none;
  color: var(--text-2);
  cursor: pointer;
  border-radius: var(--radius);
  transition: all 0.12s;
  text-align: left;
  width: 100%;
}

.nav-item:hover {
  background: var(--bg-hover);
  color: var(--text-0);
}

.nav-item.active {
  background: var(--accent-bg);
  color: var(--accent-text);
  font-weight: 600;
  box-shadow: var(--shadow-card);
}

.nav-advanced {
  padding: 12px 8px;
  border-top: 1px solid rgba(27, 41, 64, 0.08);
  border-bottom: 1px solid rgba(27, 41, 64, 0.08);
}

.advanced-toggle {
  display: grid;
  grid-template-columns: 1fr auto auto;
  align-items: center;
  gap: 10px;
  font-size: 12px;
  color: var(--text-2);
}

.advanced-toggle input {
  display: none;
}

.advanced-slider {
  position: relative;
  width: 38px;
  height: 22px;
  border-radius: 999px;
  background: rgba(27, 41, 64, 0.12);
  transition: background 0.18s ease;
}

.advanced-slider::after {
  content: '';
  position: absolute;
  top: 3px;
  left: 3px;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 2px 6px rgba(18, 24, 38, 0.18);
  transition: transform 0.18s ease;
}

.advanced-toggle input:checked+.advanced-slider {
  background: var(--accent);
}

.advanced-toggle input:checked+.advanced-slider::after {
  transform: translateX(16px);
}

.advanced-note {
  margin: 8px 0 0;
  font-size: 11px;
  line-height: 1.45;
  color: var(--text-3);
}

.settings-content {
  flex: 1;
  overflow: hidden;
}

.settings-scroll {
  height: 100%;
  overflow-y: auto;
  padding: 36px 48px;
  max-width: 840px;
  margin: 0 auto;
  animation: fadeUp 0.3s var(--ease-out);
}

.settings-head {
  margin-bottom: 24px;
}

.settings-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.settings-brand-mark {
  width: 42px;
  height: 42px;
  border-radius: 15px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(242, 247, 255, 0.9));
  box-shadow: var(--shadow-sm);
  display: flex;
  align-items: center;
  justify-content: center;
}

.settings-brand-logo {
  width: 26px;
  height: 26px;
  object-fit: contain;
  display: block;
}

.settings-brand-fallback {
  font-family: var(--font-display);
  font-size: 20px;
  font-weight: 700;
  color: var(--accent-text);
  line-height: 1;
}

.settings-brand-copy {
  display: flex;
  flex-direction: column;
  gap: 3px;
  line-height: 1;
}

.settings-brand-kicker {
  font-size: 10px;
  font-weight: 700;
  color: var(--text-3);
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.settings-brand-name {
  font-size: 16px;
  font-weight: 700;
  color: var(--text-1);
  font-family: var(--font-display);
}

.settings-title {
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.settings-desc {
  font-size: 13px;
  color: var(--text-2);
  margin-top: 4px;
}

/* AI Config */
.setup-panel {
  padding: 18px 18px 16px;
  margin-bottom: 18px;
}

.setup-panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
}

.setup-panel-head.compact {
  margin-bottom: 12px;
}

.setup-kicker {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--text-3);
  margin-bottom: 4px;
}

.setup-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--text-0);
}

.setup-desc {
  font-size: 12px;
  color: var(--text-2);
  margin-top: 4px;
}

.preset-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.preset-grid.compact {
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-top: 8px;
}

.preset-card {
  border: 1px solid var(--border);
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.82);
  padding: 12px 13px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.preset-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.preset-service {
  font-size: 12px;
  font-weight: 600;
}

.preset-model {
  font-size: 12px;
  color: var(--text-1);
}

.preset-base {
  font-size: 11px;
  color: var(--text-3);
}

.template-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.template-type-chip {
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.82);
  color: var(--text-1);
  border-radius: 999px;
  padding: 8px 12px;
  font-size: 12px;
  cursor: pointer;
  transition: 0.15s;
}

.template-type-chip:hover {
  border-color: var(--accent);
  color: var(--accent-text);
  background: var(--accent-bg);
}

.sections {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.section-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.section-title {
  font-size: 13px;
  font-weight: 600;
}

.section-subtitle {
  font-size: 11px;
  color: var(--text-3);
  margin-top: 2px;
}

.config-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.config-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
}

.config-info {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.config-main {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.config-line {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.config-provider {
  font-size: 13px;
  font-weight: 600;
}

.config-name {
  font-size: 12px;
  color: var(--text-2);
}

.config-model {
  font-size: 11px;
  color: var(--text-2);
}

.config-base {
  font-size: 11px;
  color: var(--text-3);
}

.config-empty {
  font-size: 12px;
  color: var(--text-3);
  padding: 12px 0;
}

.toggle {
  position: relative;
  width: 30px;
  height: 17px;
  cursor: pointer;
  flex-shrink: 0;
}

.toggle input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle span {
  position: absolute;
  inset: 0;
  background: var(--bg-3);
  border-radius: 99px;
  transition: 0.2s;
}

.toggle span::before {
  content: '';
  position: absolute;
  width: 13px;
  height: 13px;
  left: 2px;
  bottom: 2px;
  background: var(--bg-0);
  border-radius: 50%;
  transition: 0.2s;
  box-shadow: var(--shadow);
}

.toggle input:checked+span {
  background: var(--accent);
}

.toggle input:checked+span::before {
  transform: translateX(13px);
}

/* Agent */
.agent-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.agent-card {
  overflow: hidden;
}

.agent-card-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 16px;
  cursor: pointer;
  transition: background 0.1s;
}

.agent-card-head:hover {
  background: var(--bg-hover);
}

.agent-type-badge {
  width: 36px;
  height: 36px;
  border-radius: var(--radius);
  background: var(--accent-bg);
  color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}

.agent-card-body {
  padding: 0 16px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  border-top: 1px solid var(--border);
  padding-top: 16px;
}

.agent-card-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-top: 8px;
}

/* Skills 布局 */
.skills-layout {
  display: flex;
  height: 100%;
  overflow: hidden;
}

.skills-agent-list {
  width: 200px;
  flex-shrink: 0;
  border-right: 1px solid var(--border);
  background: var(--bg-1);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.skills-agent-title {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--text-3);
  padding: 14px 14px 8px;
}

.skills-agent-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 14px;
  font-size: 13px;
  cursor: pointer;
  border: none;
  background: none;
  color: var(--text-2);
  transition: all 0.12s;
  width: 100%;
  text-align: left;
  border-radius: 0;
}

.skills-agent-item:hover {
  background: var(--bg-hover);
  color: var(--text-0);
}

.skills-agent-item.active {
  background: var(--accent-bg);
  color: var(--accent-text);
  font-weight: 600;
}

.skills-agent-label {
  flex: 1;
}

.skill-count-badge {
  font-size: 10px;
  font-weight: 700;
  font-family: var(--font-mono);
  background: var(--accent-bg);
  color: var(--accent-text);
  padding: 1px 5px;
  border-radius: 99px;
}

.skills-agent-item.active .skill-count-badge {
  background: rgba(255, 255, 255, 0.2);
  color: inherit;
}

.skills-main {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.skills-main .settings-scroll {
  max-width: 900px;
}

/* Skill */
.skill-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.skill-card {
  overflow: hidden;
}

.skill-card-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  cursor: pointer;
  transition: background 0.1s;
}

.skill-card-head:hover {
  background: var(--bg-hover);
}

.skill-card-body {
  padding: 0 16px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  border-top: 1px solid var(--border);
  padding-top: 12px;
}

.skill-card-foot {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* Shared */
.field {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.field-label {
  font-size: 12px;
  font-weight: 500;
  color: var(--text-1);
}

.field-hint {
  font-size: 11px;
  color: var(--text-3);
  margin-top: 2px;
}

.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.overlay {
  position: fixed;
  inset: 0;
  background: rgba(34, 45, 66, 0.32);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  animation: fadeIn 0.18s var(--ease-out);
}

.modal {
  padding: 28px;
  width: 420px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  box-shadow: var(--shadow-elevated);
}

.modal-title {
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 6px;
}

.config-modal {
  width: min(720px, calc(100vw - 40px));
  max-height: calc(100vh - 48px);
  overflow-y: auto;
}

.config-modal-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.modal-note {
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-2);
}

.preset-picker {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.preset-pill {
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.72);
  color: var(--text-1);
  border-radius: 999px;
  padding: 8px 11px;
  font-size: 12px;
  cursor: pointer;
}

.preset-pill:hover {
  border-color: var(--accent);
  background: var(--accent-bg);
  color: var(--accent-text);
}

.endpoint-hint {
  margin-top: -4px;
  padding: 10px 12px;
  border-radius: 12px;
  border: 1px dashed var(--border);
  background: rgba(244, 248, 255, 0.72);
  font-size: 12px;
}

.test-result {
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-radius: 14px;
  padding: 12px;
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.72);
}

.test-result.ok {
  border-color: rgba(74, 167, 92, 0.28);
}

.test-result.bad {
  border-color: rgba(201, 88, 68, 0.28);
}

.test-result-head {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--text-1);
}

.test-result-url,
.test-result-preview {
  font-size: 11px;
  color: var(--text-3);
  word-break: break-all;
}

.huobao-grid {
  display: grid;
  grid-template-columns: repeat(1, minmax(0, 1fr));
  gap: 10px;
}

.huobao-grid .field-hint a {
  color: var(--accent);
  text-decoration: none;
  font-weight: 500;
}

.huobao-grid .field-hint a:hover {
  text-decoration: underline;
}

@media (max-width: 900px) {

  .preset-grid,
  .preset-grid.compact {
    grid-template-columns: 1fr;
  }
}
</style>
