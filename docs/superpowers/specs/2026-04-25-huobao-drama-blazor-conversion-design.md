# Huobao Drama Blazor Server 转换设计文档

## 概述

将原 TypeScript 全栈 AI 短剧平台（Huobao Drama）1:1 转换为 .NET 10 Blazor Server 应用，使用 Microsoft.Extensions.AI.Agents 替代 Mastra，保持 SQLite 数据库，UI 从暗色主题转为浅色主题。

## 技术栈

| 层级 | 技术 |
|------|------|
| 框架 | .NET 10 Blazor Server |
| 数据库 | SQLite (EF Core + Microsoft.Data.Sqlite) |
| AI Agents | Microsoft.Extensions.AI.Agents |
| AI 配置 | Microsoft.Extensions.AI (IChatClient) |
| 图片处理 | SkiaSharp |
| 视频处理 | FFmpeg (System.Diagnostics.Process) |
| 样式 | 纯 CSS + CSS Variables (浅色主题) |
| 文件存储 | 本地文件系统 |

## 项目结构

```
WuShuangDrama/
├── Models/                       # EF Core 实体（匹配现有 SQLite 表）
│   ├── Drama.cs
│   ├── Episode.cs
│   ├── Character.cs
│   ├── Scene.cs
│   ├── Storyboard.cs
│   ├── StoryboardCharacter.cs
│   ├── EpisodeCharacter.cs
│   ├── EpisodeScene.cs
│   ├── AiServiceConfig.cs
│   ├── AiServiceProvider.cs
│   ├── AiVoice.cs
│   ├── AgentConfig.cs
│   ├── ImageGeneration.cs
│   ├── VideoGeneration.cs
│   ├── VideoMerge.cs
│   ├── Prop.cs
│   └── Asset.cs
├── Data/
│   ├── DramaDbContext.cs         # EF Core DbContext
│   └── drama_generator.db        # SQLite 数据库文件
├── Agents/                       # Microsoft.Extensions.AI.Agents
│   ├── AgentFactory.cs           # Agent 工厂
│   ├── AgentInstructions.cs      # Agent 指令常量
│   ├── Tools/
│   │   ├── ScriptTools.cs        # 剧本读写工具
│   │   ├── ExtractTools.cs       # 角色场景提取工具
│   │   ├── StoryboardTools.cs    # 分镜工具
│   │   ├── VoiceTools.cs         # 音色工具
│   │   └── GridPromptTools.cs    # 宫格提示词工具
│   └── Skills.cs                 # Skill 加载
├── Services/
│   ├── AIService.cs              # AI 配置管理
│   ├── ImageService.cs           # 图片生成
│   ├── VideoService.cs           # 视频生成
│   ├── TTSService.cs             # TTS 语音合成
│   ├── ComposeService.cs         # FFmpeg 镜头合成
│   ├── MergeService.cs           # FFmpeg 拼接
│   ├── GridService.cs            # 宫格图生成/切分
│   └── StorageService.cs         # 文件存储
├── Adapters/                     # 多厂商适配器
│   ├── IImageProvider.cs
│   ├── IVideoProvider.cs
│   ├── ITTSProvider.cs
│   ├── Image/
│   │   ├── OpenAIProvider.cs
│   │   ├── GeminiProvider.cs
│   │   ├── MiniMaxProvider.cs
│   │   ├── VolcEngineProvider.cs
│   │   └── AliProvider.cs
│   ├── Video/
│   │   ├── MiniMaxProvider.cs
│   │   ├── VolcEngineProvider.cs
│   │   ├── VidyuProvider.cs
│   │   └── AliProvider.cs
│   ├── TTS/
│   │   └── MiniMaxProvider.cs
│   └── AdapterRegistry.cs
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # 壳布局（浅色）
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor         # 顶部导航
│   │   └── NavMenu.razor.css
│   ├── Pages/
│   │   ├── Index.razor           # 项目列表
│   │   ├── Index.razor.css
│   │   ├── DramaDetail.razor     # 剧本详情
│   │   ├── DramaDetail.razor.css
│   │   ├── Workbench.razor       # 工作台
│   │   ├── Workbench.razor.css
│   │   ├── Settings.razor        # 设置
│   │   └── Settings.razor.css
│   └── Shared/
│       ├── BaseSelect.razor
│       ├── Modal.razor
│       ├── ProgressBar.razor
│       ├── PipelineStatus.razor
│       └── StoryboardEditor.razor
├── wwwroot/
│   └── css/
│       └── app.css               # 全局浅色主题
├── Program.cs                    # 入口
├── appsettings.json              # 基础配置
├── appsettings.Development.json
└── WuShuangDrama.csproj          # .NET 10 SDK
```

## 数据库映射

保留现有 SQLite 表结构，使用 EF Core 逆向工程或手写实体类精确匹配。

### 表清单

| 表名 | 实体 | 说明 |
|------|------|------|
| dramas | Drama | 剧本项目 |
| episodes | Episode | 剧集 |
| characters | Character | 角色 |
| episode_characters | EpisodeCharacter | 集-角色多对多 |
| episode_scenes | EpisodeScene | 集-场景多对多 |
| scenes | Scene | 场景 |
| storyboards | Storyboard | 分镜 |
| storyboard_characters | StoryboardCharacter | 分镜-角色多对多 |
| ai_service_configs | AiServiceConfig | AI 服务配置 |
| ai_service_providers | AiServiceProvider | AI 服务厂商 |
| ai_voices | AiVoice | AI 音色 |
| agent_configs | AgentConfig | Agent 配置 |
| image_generations | ImageGeneration | 图片生成记录 |
| video_generations | VideoGeneration | 视频生成记录 |
| video_merges | VideoMerge | 视频拼接记录 |
| props | Prop | 道具 |
| assets | Asset | 资源 |

## Agent 替换方案

### 架构

使用 `Microsoft.Extensions.AI` 命名空间中的核心抽象：

- `IChatClient` — 统一 AI 聊天客户端接口
- `ChatOptions` — 聊天选项（model, temperature, tools 等）
- `AIChatAgent` — AI Agent 
- `AITool` — Agent 工具基类
- `ChatMessage` / `ChatRole` — 消息模型

### 5 个 Agent 映射

| 原 Mastra Agent | MAF Agent | Tools |
|----------------|-----------|-------|
| script_rewriter | ScriptRewriterAgent | read_episode_script, save_script |
| extractor | ExtractorAgent | read_script_for_extraction, read_existing_characters, read_existing_scenes, save_dedup_characters, save_dedup_scenes |
| storyboard_breaker | StoryboardBreakerAgent | read_storyboard_context, save_storyboards |
| voice_assigner | VoiceAssignerAgent | list_voices, get_characters, assign_voice |
| grid_prompt_generator | GridPromptGeneratorAgent | read_characters, read_scenes, read_shots_for_grid, generate_grid_prompt |

### Agent 工厂示例

```csharp
public class AgentFactory
{
    private readonly IServiceProvider _services;
    private readonly DramaDbContext _db;

    public AIChatAgent CreateAgent(string type, int episodeId, int dramaId)
    {
        var config = _db.AgentConfigs.FirstOrDefault(c => c.AgentType == type && c.IsActive);
        var textConfig = AIService.GetActiveConfig("text");
        
        var client = new OpenAIClient(new HttpClient(), new OpenAIClientOptions
        {
            Endpoint = new Uri(textConfig.BaseUrl),
            ApiKey = textConfig.ApiKey
        });

        var chatClient = client.AsChatClient(textConfig.Model);
        var instructions = BuildInstructions(config, type, episodeId, dramaId);
        var tools = BuildTools(type, episodeId, dramaId);
        var options = new ChatOptions { Tools = tools.Cast<AITool>().ToList() };

        return new AIChatClientAgent(chatClient, type, instructions, options);
    }
}
```

## 适配器模式（多厂商 AI）

保持原项目的适配器模式，将其从 TypeScript 转换为 C# 接口：

```csharp
public interface IImageProvider
{
    string Provider { get; }
    ProviderRequest BuildGenerateRequest(AIConfig config, ImageGenerationRecord record);
    ImageGenResponse ParseGenerateResponse(JsonElement result);
    ProviderRequest BuildPollRequest(AIConfig config, string taskId);
    ImagePollResponse ParsePollResponse(JsonElement result);
    string? ExtractImageUrl(JsonElement result);
    (string Data, string MimeType)? ExtractImageBase64(JsonElement result);
}

public interface IVideoProvider
{
    string Provider { get; }
    ProviderRequest BuildGenerateRequest(AIConfig config, VideoGenerationRecord record);
    VideoGenResponse ParseGenerateResponse(JsonElement result);
    ProviderRequest BuildPollRequest(AIConfig config, string taskId);
    VideoPollResponse ParsePollResponse(JsonElement result);
}

public interface ITTSProvider
{
    string Provider { get; }
    ProviderRequest BuildGenerateRequest(AIConfig config, TTSParams parameters);
    TTSResponse ParseResponse(JsonElement result);
}
```

## 浅色主题设计

### 颜色变量

```css
:root {
  /* 背景 */
  --bg-0: #ffffff;           /* 主背景 - 白 */
  --bg-1: #f8f9fb;           /* 次要背景 - 浅灰 */
  --bg-2: #f0f1f4;           /* 卡片背景 - 中灰 */
  --bg-hover: #e8eaed;       /* 悬停背景 */
  --bg-base: #f5f6f8;        /* 页面背景 */
  
  /* 文字 */
  --text-0: #1a1d23;         /* 主文字 - 黑 */
  --text-1: #2c2f36;         /* 次要文字 */
  --text-2: #5a5f6b;         /* 辅助文字 */
  --text-3: #8e93a0;         /* 提示文字 */
  
  /* 强调色 */
  --accent: #4c7dff;         /* 蓝色主色 */
  --accent-bg: #eef2ff;      /* 蓝色背景 */
  --accent-text: #3b6ee8;    /* 蓝色文字 */
  --accent-gradient: linear-gradient(135deg, #4c7dff, #6b5bff);
  
  /* 语义色 */
  --success: #22c55e;
  --warning: #f59e0b;
  --error: #ef4444;
  
  /* 边框 */
  --border: #e2e5eb;
  --border-strong: #c8ccd4;
  
  /* 阴影 */
  --shadow-xs: 0 1px 2px rgba(0,0,0,0.04);
  --shadow: 0 2px 8px rgba(0,0,0,0.06);
  --shadow-lg: 0 8px 24px rgba(0,0,0,0.08);
  --shadow-elevated: 0 20px 48px rgba(0,0,0,0.12);
}
```

## 路由设计

| 路由 | 组件 | 说明 |
|------|------|------|
| `/` | `Index.razor` | 项目列表 + 新建弹窗 |
| `/drama/{DramaId:int}` | `DramaDetail.razor` | 剧本详情 + 剧集列表 |
| `/drama/{DramaId:int}/episode/{EpisodeNumber:int}` | `Workbench.razor` | 单集工作台（核心页面） |
| `/settings` | `Settings.razor` | AI 服务配置 + Agent 配置 |

## 服务层设计

### AIService
- `GetActiveConfig(ServiceType)` — 按服务类型获取活跃配置
- `GetConfigById(int id)` — 按 ID 获取配置
- `TestConnection(AiServiceConfig)` — 测试 AI 服务连接

### ImageService
- `GenerateImage(ImageParams)` — 生成图片（同步/异步）
- `PollImageTask(int id)` — 轮询异步任务
- `NormalizeReferenceImages(string[])` — 参考图转 Base64

### VideoService
- `GenerateVideo(VideoParams)` — 生成视频
- `PollVideoTask(int id)` — 轮询异步任务

### TTSService
- `GenerateTTS(TTSParams)` — 生成 TTS 音频
- `GenerateVoiceSample(name, voiceId)` — 角色试听

### ComposeService
- `ComposeStoryboard(int storyboardId)` — 合成单镜头（视频+音频+字幕）

### MergeService
- `MergeEpisodeVideos(int episodeId)` — 拼接全集视频

### GridService
- `GenerateGridPrompt(...)` — 宫格图提示词
- `GenerateGridImage(...)` — 生成宫格图
- `SplitGridImage(...)` — 切分宫格图

### StorageService
- `DownloadFile(url, subDir)` — 下载远程文件
- `SaveUploadedFile(data, subDir, name)` — 保存上传文件
- `ReadImageAsCompressedDataUrl(path)` — 图片转压缩 Base64

## SignalR 流式更新

工作台中的生成任务进度使用 Blazor Server 的 SignalR 连接实时更新：

```
Agent 对话 → IChatClient.CompleteStreamingAsync() → 流式输出到 UI
生成任务 → 后台 Task.Run → 定期更新 DB → UI 轮询刷新
```

## FFmpeg 集成

保持与原项目相同的 FFmpeg 命令行调用模式，使用 `System.Diagnostics.Process`：

```csharp
public class ComposeService
{
    public async Task<string> ComposeStoryboard(int storyboardId)
    {
        var arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v libx264 ...";
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        await process.WaitForExitAsync();
        // ...
    }
}
```

## 实现顺序

### Phase 1: 项目骨架 & 数据库
1. 升级 csproj 到 .NET 10
2. 定义 EF Core 实体模型
3. 创建 DbContext
4. 创建 Program.cs 入口（DI 注册）

### Phase 2: 浅色主题 & 布局
1. 编写全局 CSS（浅色颜色变量）
2. 创建 MainLayout + NavMenu
3. 实现路由

### Phase 3: 基础页面
1. Index.razor — 项目列表 + CRUD
2. DramaDetail.razor — 剧本详情 + 剧集管理

### Phase 4: 工作台（核心）
1. Workbench.razor — 左侧流水线导航
2. 右侧内容面板（分镜/角色/场景/视频）
3. 流水线进度组件

### Phase 5: 设置页
1. AI 服务配置管理
2. Agent 配置管理
3. 火宝预设配置
4. 连接测试功能

### Phase 6: AI Agent 层
1. AgentFactory + MAF 集成
2. 5 个 Agent 实现
3. Agent Tools 实现

### Phase 7: 服务层
1. AIService
2. ImageService + 适配器
3. VideoService + 适配器
4. TTSService + 适配器

### Phase 8: 视频处理
1. ComposeService (FFmpeg)
2. MergeService (FFmpeg)
3. GridService (宫格图)

### Phase 9: 文件上传 & 存储
1. StorageService
2. 上传组件
3. 静态文件服务

### Phase 10: 集成测试 & 完善
