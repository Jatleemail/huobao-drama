# 角色与场景 CRUD 功能设计

## 概述

在"提取角色与场景"页面（Step 2）中，为角色和场景列表增加手动新增、编辑、删除功能，使用户可以在 AI 自动提取之外手动管理角色和场景数据。

## 设计决策

- **方案**：最小侵入内联 UI 扩展，遵循现有 overlay + card 对话框模式
- **角色主配角选项**：主角、配角、其他
- **场景描述字段映射**：映射到 scenes 表的 `prompt` 字段

## 后端变更

### 文件：`backend/src/routes/characters.ts`

#### 新增 `POST /api/v1/characters` — 创建角色

```
Method: POST
Path: /api/v1/characters
Body: { drama_id: number, episode_id: number, name: string, role?: string, description?: string }
Response: 201 { code: 0, data: { id, name, role, description, ... }, message: "ok" }
```

- `drama_id` 和 `name` 必填
- `episode_id` 必填，用于自动关联到当前 episode（写入 `episode_characters` 关联表）
- `role` 和 `description` 可选

#### 新增 `GET /api/v1/characters/:id/storyboard-bindings` — 检查分镜绑定

```
Method: GET
Path: /api/v1/characters/:id/storyboard-bindings
Response: { code: 0, data: { bound: boolean, storyboard_count: number } }
```

- 查询 `storyboard_characters` 表，统计引用该角色的分镜数量

### 文件：`backend/src/routes/scenes.ts`

#### 新增 `GET /api/v1/scenes/:id/storyboard-bindings` — 检查分镜绑定

```
Method: GET
Path: /api/v1/scenes/:id/storyboard-bindings
Response: { code: 0, data: { bound: boolean, storyboard_count: number } }
```

- 查询 `storyboards` 表，统计 `scene_id` 引用该场景的分镜数量

#### 修改 `DELETE /api/v1/scenes/:id` — 增加绑定检查

- 删除前检查分镜绑定，若 `bound_count > 0` 则返回 400 错误："场景已被 N 个分镜引用，无法删除"

### 文件：`backend/src/db/schema.ts`

无需修改。现有字段已满足需求：
- `characters` 表：`name`、`role`、`description` 均已存在
- `scenes` 表：`location`（场景名称）、`time`（时间）、`prompt`（场景描述）均已存在

## 前端变更

### 文件：`frontend/app/composables/useApi.ts`

在现有 API 对象中新增方法：

```typescript
// characterAPI 新增
create: (data: { drama_id: number; episode_id: number; name: string; role?: string; description?: string }) =>
  api.post('/characters', data),
del: (id: number) => api.del(`/characters/${id}`),
storyboardBindings: (id: number) => api.get<{ bound: boolean; storyboard_count: number }>(`/characters/${id}/storyboard-bindings`),

// sceneAPI 新增
create: (data: { drama_id: number; episode_id?: number; location: string; time?: string; prompt?: string }) =>
  api.post('/scenes', data),
update: (id: number, data: { location?: string; time?: string; prompt?: string }) =>
  api.put(`/scenes/${id}`, data),
del: (id: number) => api.del(`/scenes/${id}`),
storyboardBindings: (id: number) => api.get<{ bound: boolean; storyboard_count: number }>(`/scenes/${id}/storyboard-bindings`),
```

### 文件：`frontend/app/pages/drama/[id]/episode/[episodeNumber].vue`

这是核心变更文件。所有变更都在 `<script setup>` 和 Step 2 模板区域内。

#### 图标导入新增

从 `lucide-vue-next` 增加导入 `Plus`、`Trash2`。

#### 新状态变量

```typescript
// 对话框开关
const charDialog = ref(false)
const sceneDialog = ref(false)

// 编辑目标（null = 新增，非 null = 编辑）
const editingChar = ref(null)
const editingScene = ref(null)

// 表单数据
const charForm = reactive({ name: '', role: '', description: '' })
const sceneForm = reactive({ location: '', time: '', prompt: '' })

// 删除确认目标
const deleteConfirm = ref(null) // { type: 'char' | 'scene', item: object }
```

#### 角色对话框逻辑

```typescript
function openCharDialog(char = null) {
  editingChar.value = char
  if (char) {
    charForm.name = char.name || ''
    charForm.role = char.role || ''
    charForm.description = char.description || ''
  } else {
    charForm.name = ''
    charForm.role = ''
    charForm.description = ''
  }
  charDialog.value = true
}

async function saveChar() {
  if (!charForm.name.trim()) { toast.error('请输入角色名称'); return }
  try {
    if (editingChar.value) {
      await characterAPI.update(editingChar.value.id, { name: charForm.name, role: charForm.role, description: charForm.description })
      toast.success('角色已更新')
    } else {
      await characterAPI.create({ drama_id: dramaId, episode_id: epId.value, name: charForm.name, role: charForm.role, description: charForm.description })
      toast.success('角色已创建')
    }
    charDialog.value = false
    await refresh()
  } catch (e) { toast.error(e.message) }
}
```

#### 场景对话框逻辑

```typescript
function openSceneDialog(scene = null) {
  editingScene.value = scene
  if (scene) {
    sceneForm.location = scene.location || ''
    sceneForm.time = scene.time || ''
    sceneForm.prompt = scene.prompt || ''
  } else {
    sceneForm.location = ''
    sceneForm.time = ''
    sceneForm.prompt = ''
  }
  sceneDialog.value = true
}

async function saveScene() {
  if (!sceneForm.location.trim()) { toast.error('请输入场景名称'); return }
  try {
    if (editingScene.value) {
      await sceneAPI.update(editingScene.value.id, { location: sceneForm.location, time: sceneForm.time, prompt: sceneForm.prompt })
      toast.success('场景已更新')
    } else {
      await sceneAPI.create({ drama_id: dramaId, episode_id: epId.value, location: sceneForm.location, time: sceneForm.time, prompt: sceneForm.prompt })
      toast.success('场景已创建')
    }
    sceneDialog.value = false
    await refresh()
  } catch (e) { toast.error(e.message) }
}
```

#### 删除逻辑

```typescript
async function confirmDeleteChar(char) {
  try {
    const { bound, storyboard_count } = await characterAPI.storyboardBindings(char.id)
    if (bound) {
      toast.error(`无法删除角色"${char.name}"，已被 ${storyboard_count} 个分镜引用`)
    } else {
      deleteConfirm.value = { type: 'char', item: char }
    }
  } catch (e) { toast.error(e.message) }
}

async function confirmDeleteScene(scene) {
  try {
    const { bound, storyboard_count } = await sceneAPI.storyboardBindings(scene.id)
    if (bound) {
      toast.error(`无法删除场景"${scene.location}"，已被 ${storyboard_count} 个分镜引用`)
    } else {
      deleteConfirm.value = { type: 'scene', item: scene }
    }
  } catch (e) { toast.error(e.message) }
}

async function doDelete() {
  if (!deleteConfirm.value) return
  const { type, item } = deleteConfirm.value
  try {
    if (type === 'char') {
      await characterAPI.del(item.id)
      toast.success(`角色"${item.name}"已删除`)
    } else {
      await sceneAPI.del(item.id)
      toast.success(`场景"${item.location}"已删除`)
    }
    deleteConfirm.value = null
    await refresh()
  } catch (e) { toast.error(e.message) }
}
```

#### 模板变更

**角色区域标题（第 269 行附近）**：在 `<span>角色</span>` 后增加 `+角色` 按钮：

```html
<button class="btn btn-sm" style="margin-left:auto" @click="openCharDialog()">
  <Plus :size="12" /> 角色
</button>
```

**角色行（第 274-283 行附近）**：在 `.extract-row` 末尾增加操作按钮：

```html
<div class="extract-row-actions">
  <button class="btn-icon" title="编辑角色" @click="openCharDialog(c)"><Pencil :size="12" /></button>
  <button class="btn-icon" title="删除角色" @click="confirmDeleteChar(c)"><Trash2 :size="12" /></button>
</div>
```

**场景区域标题（第 288 行附近）**：同角色模式增加 `+场景` 按钮。

**场景行（第 294-305 行附近）**：同角色模式增加编辑/删除按钮。

**角色编辑对话框**：在 Step 2 区域末尾（`</div>` 关闭前）添加：

```html
<div v-if="charDialog" class="overlay" @click.self="charDialog = false">
  <div class="card" style="width:420px;max-width:95vw;padding:20px">
    <div style="font-size:15px;font-weight:600;font-family:var(--font-display);margin-bottom:12px">
      {{ editingChar ? '编辑角色' : '新增角色' }}
    </div>
    <label class="field">
      <span class="field-label">角色名称</span>
      <input class="input" v-model="charForm.name" placeholder="输入角色名称" />
    </label>
    <label class="field">
      <span class="field-label">角色描述</span>
      <textarea class="textarea" v-model="charForm.description" rows="3" placeholder="角色外貌、性格、背景等" />
    </label>
    <label class="field">
      <span class="field-label">主配角</span>
      <select class="input" v-model="charForm.role">
        <option value="">请选择</option>
        <option value="主角">主角</option>
        <option value="配角">配角</option>
        <option value="其他">其他</option>
      </select>
    </label>
    <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:16px">
      <button class="btn" @click="charDialog = false">取消</button>
      <button class="btn btn-primary" @click="saveChar">确认</button>
    </div>
  </div>
</div>
```

**场景编辑对话框**：同角色模式，字段为场景名称（location）、场景描述（prompt）、时间（time）。

**删除确认对话框**：

```html
<div v-if="deleteConfirm" class="overlay" @click.self="deleteConfirm = null">
  <div class="card" style="width:360px;max-width:95vw;padding:20px;text-align:center">
    <div style="font-size:15px;font-weight:600;margin-bottom:8px">确认删除</div>
    <div class="dim" style="margin-bottom:16px">
      确定要删除{{ deleteConfirm.type === 'char' ? '角色' : '场景' }}「{{ deleteConfirm.type === 'char' ? deleteConfirm.item.name : deleteConfirm.item.location }}」吗？此操作不可撤销。
    </div>
    <div style="display:flex;justify-content:center;gap:8px">
      <button class="btn" @click="deleteConfirm = null">取消</button>
      <button class="btn btn-danger" @click="doDelete">确认删除</button>
    </div>
  </div>
</div>
```

#### CSS 新增

在 `<style scoped>` 中添加行操作按钮样式：

```css
.extract-row {
  position: relative;
}
.extract-row-actions {
  display: none;
  gap: 2px;
  align-items: center;
  margin-left: auto;
  flex-shrink: 0;
}
.extract-row:hover .extract-row-actions {
  display: flex;
}
.btn-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  background: transparent;
  color: var(--text-dim);
  border-radius: 6px;
  cursor: pointer;
}
.btn-icon:hover {
  background: var(--bg-hover);
  color: var(--text);
}
.btn-danger {
  background: #dc2626;
  color: #fff;
}
.btn-danger:hover {
  background: #b91c1c;
}
```

## 状态管理流程

```
openCharDialog(null) → charDialog=true, editingChar=null, charForm={} → 用户填写 → saveChar()
  → characterAPI.create() → refresh() → chars 列表刷新

openCharDialog(c) → charDialog=true, editingChar=c, charForm={c的数据} → 用户修改 → saveChar()
  → characterAPI.update() → refresh() → chars 列表刷新

confirmDeleteChar(c) → characterAPI.storyboardBindings(c.id)
  → bound=true  → toast.error("无法删除")
  → bound=false → deleteConfirm={type:'char',item:c} → 用户确认 → doDelete()
    → characterAPI.del() → refresh()
```

场景同理。

## 验证步骤

1. **后端验证**：
   - `POST /api/v1/characters` 创建角色并自动关联 episode
   - `GET /api/v1/characters/:id/storyboard-bindings` 返回正确的绑定状态
   - `GET /api/v1/scenes/:id/storyboard-bindings` 返回正确的绑定状态
   - 删除被分镜引用的场景返回 400 错误
   - 删除未被引用的场景成功

2. **前端验证**：
   - 点击 "+角色" 按钮打开空表单对话框
   - 填写表单并确认，角色出现在列表中
   - 点击角色行编辑按钮，打开预填表单，修改并保存
   - 点击角色行删除按钮，未被绑定的角色可删除
   - 已被分镜引用的角色/场景删除时弹出提示
   - 场景的创建/编辑/删除同理

3. **端到端验证**：
   - 启动前后端服务，在浏览器中打开剧集页面的 Step 2
   - 完成上述所有操作的手动测试
