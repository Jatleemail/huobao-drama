---
name: grid_prompt_generator
description: 图片提示词生成指南 — 角色、场景、宫格图三类提示词规范
agent_type: grid_prompt_generator
---

# 图片提示词生成指南

本 SKILL 对应 `grid_prompt_generator` Agent，提供角色、场景、宫格图三类提示词的**详细模板和完整示例**。

> 工作流程和通用规范见 Agent system prompt（DEFAULT_PROMPTS），本文件聚焦模板细节和实例。

1. **角色图片提示词** — 角色外貌与气质
2. **场景图片提示词** — 场景氛围与光线
3. **宫格图提示词** — 多镜头网格拼图

详细模板见 `reference/` 目录。

---

## 一、角色图片提示词

参考：`reference/character-prompt.md`

### 模板结构
```
[Gender + age] character, [name], [body type], [facial features].
[Hair description — length/color/style].
[Clothing/Accessories — style/color/accessories].
[Pose and expression matching personality].
Background: [simple/gradient/neutral], no distractions.
Style: [project visual style], high quality, detailed, character concept art, consistent art style, no text, no watermark.
```

### 完整示例
```
A young male character in his late teens, Xiao Heshang, slender build,
gentle oval face with bright sincere eyes and a warm smile.
Short black hair, neatly shaved. Wearing a simple grey Buddhist robe
with brown wooden beads on left wrist, straw sandals.
Standing with hands clasped in prayer, eyes lowered reverently,
soft morning light falling on face.
Background: soft misty gradient with distant mountain silhouette.
Style: chinoiserie ink wash, high quality, detailed, character concept art,
consistent art style, no text, no watermark.
```

### 生成规则
- 以 `appearance`（外貌描述）为核心，不凭空编造
- `personality` 在表情和姿态中体现
- `role` 决定服装和道具风格
- 必须包含项目视觉风格标签 + `character concept art`
- 必须包含 `consistent art style` + `no text, no watermark`

### 质量自检
- □ 外貌基于 appearance 字段
- □ personality 在 pose/expression 中体现
- □ 包含项目视觉风格标签
- □ 明确禁止文字和水印

---

## 二、场景图片提示词

参考：`reference/scene-prompt.md`

### 模板结构
```
A [visual style] pure background scene of [location] at [time period].
The scene shows [environment details — architecture/objects/lighting/atmosphere].
No characters, no people, no figures, no silhouettes.
Style: [visual style], rich details, atmospheric lighting, high quality, consistent art style, no text, no watermark.
Mood: [2-3 words specific mood].
```

### 完整示例
```
A chinoiserie ink wash pure background scene of a mountain temple courtyard at dawn.
The scene shows an ancient stone courtyard with moss-covered steps,
a bronze incense burner with curling smoke in center,
weathered wooden pillars supporting curved eaves with morning dew on tiles,
a dry water vat in corner, withered potted plants nearby.
Soft golden light filtering through morning mist, casting long gentle shadows.
No characters, no people, no figures, no silhouettes.
Style: chinoiserie ink wash, rich details, atmospheric lighting, high quality,
consistent art style, no text, no watermark.
Mood: serene, tranquil, sacred.
```

### 生成规则
- 以 `location`（地点）为基础
- `time` 决定光线色调（白天/夜晚/黄昏）
- 氛围词具体（如 peaceful, nostalgic, gloomy），不写 "good atmosphere"
- 明确声明 no characters / no people — 纯背景图
- 必须包含 `consistent art style` + `no text, no watermark`

---

## 宫格图提示词

参考：`reference/shot-prompt.md`

### 三种模式

#### 首帧模式 (first_frame)
每个格子 = 一个镜头的起始画面，但必须严格生成用户指定的 `rows x cols` 总格数。

```
[rows x cols grid layout], exactly [rows*cols] visible panels, consistent art style, [style description],
格1: [shot 1 opening scene],
格2: [shot 2 opening scene],
格3: [shot 3 opening scene],
...
格N: [opening scene],
high quality, cinematic lighting, no merged panels, no missing panels, no text, no watermark
```

#### 首尾帧模式 (first_last)
保持首尾帧节奏感，但仍然必须严格生成用户指定的 `rows x cols` 总格数，不允许偷偷改成 `Nx2`。

```
[rows x cols grid layout], exactly [rows*cols] visible panels, consistent art style, [style description],
格1: [opening beat],
格2: [closing beat],
格3: [opening beat],
格4: [closing beat],
...
high quality, cinematic, continuous motion implied, no merged panels, no missing panels, no text
```

#### 多参考模式 (multi_ref)
所有格子都是同一镜头的不同角度/构图参考，但仍然必须严格生成用户指定的 `rows x cols` 总格数。

```
[rows x cols grid layout], exactly [rows*cols] visible panels, same scene different angles, [style description],
[main scene description],
格1: wide shot establishing,
格2: medium shot character focus,
格3: close-up detail,
格4: dramatic angle,
...
consistent lighting and color palette, no merged panels, no missing panels, no text
```

### 通用规则
1. 提示词使用**英文**
2. 必须明确写出用户指定的 `rows x cols grid layout`
3. 必须包含 `consistent art style` 保持风格统一
4. 必须明确要求 `exactly N visible panels`
5. 必须明确要求 `no merged panels, no missing panels`
6. 避免在格子间出现分割线的描述
7. 尺寸建议：每格 960x540，总图 = 960×cols × 540×rows
8. 当存在参考图映射时，统一使用 `图片1/图片2/...` 指代参考图，不要把它和 `格1/格2/...` 混用
