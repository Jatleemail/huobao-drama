# Huobao Drama → Blazor Server Conversion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the TypeScript-based Huobao Drama AI short-video production platform to .NET 10 Blazor Server with 1:1 feature parity.

**Architecture:** Single Blazor Server app using SignalR for real-time UI. EF Core + SQLite for data. Microsoft.Extensions.AI.Agents (MAF) replacing Mastra. Multi-vendor AI adapter pattern preserved. FFmpeg process calls for video processing. Light/white theme replacing original dark theme.

**Tech Stack:** .NET 10, Blazor Server (InteractiveServer), EF Core + Microsoft.Data.Sqlite, Microsoft.Extensions.AI.Agents, SkiaSharp, FFmpeg, Pure CSS light theme

---

## File Structure

```
WuShuangDrama/
├── WuShuangDrama.csproj              # .NET 10 Blazor Server
├── WuShuangDrama.slnx
├── Program.cs                         # Entry + DI
├── _Imports.razor                     # Global usings
├── App.razor                          # Router
├── appsettings.json                   # Config

├── Models/                            # EF Core entities
│   ├── Drama.cs
│   ├── Episode.cs
│   ├── Character.cs
│   ├── EpisodeCharacter.cs
│   ├── EpisodeScene.cs
│   ├── Scene.cs
│   ├── Storyboard.cs
│   ├── StoryboardCharacters.cs
│   ├── AiServiceConfig.cs
│   ├── AiVoice.cs
│   ├── AgentConfig.cs
│   ├── ImageGeneration.cs
│   ├── VideoGeneration.cs
│   ├── VideoMerge.cs
│   └── Prop.cs

├── Data/
│   ├── DramaDbContext.cs              # EF Core DbContext
│   └── DbInitializer.cs              # DB auto-create

├── Agents/
│   ├── AgentFactory.cs               # Factory + DI
│   ├── AgentInstructions.cs          # Default prompts
│   ├── Skills.cs                     # Skill loading
│   ├── Tools/
│   │   ├── ScriptTools.cs
│   │   ├── ExtractTools.cs
│   │   ├── StoryboardTools.cs
│   │   ├── VoiceTools.cs
│   │   └── GridPromptTools.cs

├── Services/
│   ├── AIService.cs                  # AI config management
│   ├── ImageService.cs               # Image generation
│   ├── VideoService.cs               # Video generation
│   ├── TTSService.cs                 # TTS audio generation
│   ├── ComposeService.cs             # FFmpeg compose
│   ├── MergeService.cs               # FFmpeg merge
│   ├── GridService.cs                # Grid image generate/split
│   └── StorageService.cs             # File storage

├── Adapters/
│   ├── IImageProvider.cs             # Image adapter interface
│   ├── IVideoProvider.cs             # Video adapter interface
│   ├── ITTSProvider.cs               # TTS adapter interface
│   ├── AdapterRegistry.cs            # Registry
│   ├── ProviderModels.cs             # Shared DTOs
│   ├── Image/
│   │   ├── OpenAIImageProvider.cs
│   │   ├── GeminiImageProvider.cs
│   │   ├── MiniMaxImageProvider.cs
│   │   ├── VolcEngineImageProvider.cs
│   │   └── AliImageProvider.cs
│   ├── Video/
│   │   ├── MiniMaxVideoProvider.cs
│   │   ├── VolcEngineVideoProvider.cs
│   │   ├── ViduVideoProvider.cs
│   │   └── AliVideoProvider.cs
│   └── TTS/
│       └── MiniMaxTTSProvider.cs

├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          # App shell (light theme)
│   │   ├── MainLayout.razor.css
│   │   └── NavMenu.razor             # Top nav bar
│   ├── Pages/
│   │   ├── Index.razor               # Project list
│   │   ├── DramaDetail.razor         # Drama detail + episodes
│   │   ├── Workbench.razor           # Episode workbench (core)
│   │   └── Settings.razor            # AI configs + agents
│   └── Shared/
│       ├── BaseSelect.razor          # Dropdown select
│       └── EmptyState.razor          # Empty state display

├── wwwroot/
│   └── css/
│       └── app.css                   # Full light theme CSS
```

---

## Phase 1: Project Skeleton & EF Core Database

### Task 1.1: Upgrade project to .NET 10 Blazor Server

**Files:**
- Modify: `WuShuangDrama.csproj`
- Modify: `WuShuangDrama.slnx`

- [ ] **Update .csproj to .NET 10**

```
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>WuShuangDrama</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0-*" />
    <PackageReference Include="Microsoft.Extensions.AI" Version="10.0.0-*" />
    <PackageReference Include="Microsoft.Extensions.AI.Agents" Version="10.0.0-*" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.0.0-*" />
    <PackageReference Include="SkiaSharp" Version="3.*" />
  </ItemGroup>
</Project>
```

- [ ] **Verify SDK targets .NET 10**

Run: `dotnet --list-sdks` and ensure `10.0.x` is present.

- [ ] **Update Program.cs for Blazor Server**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<DramaDbContext>(options =>
    options.UseSqlite($"Data Source={GetDbPath()}"));

builder.Services.AddScoped<AgentFactory>();
builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<VideoService>();
builder.Services.AddScoped<TTSService>();
builder.Services.AddScoped<ComposeService>();
builder.Services.AddScoped<MergeService>();
builder.Services.AddScoped<GridService>();
builder.Services.AddScoped<StorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

DbInitializer.EnsureDbCreated(app.Services);

app.Run();
```

- [ ] **Build to verify compilation**

Run: `dotnet build` from WuShuangDrama directory

### Task 1.2: Define EF Core entity models

**Files:**
- Create: `Models/Drama.cs`
- Create: `Models/Episode.cs`
- Create: `Models/Character.cs`
- Create: `Models/Scene.cs`
- Create: `Models/Storyboard.cs`
- Create: `Models/EpisodeCharacter.cs`
- Create: `Models/EpisodeScene.cs`
- Create: `Models/StoryboardCharacters.cs`
- Create: `Models/AiServiceConfig.cs`
- Create: `Models/AiVoice.cs`
- Create: `Models/AgentConfig.cs`
- Create: `Models/ImageGeneration.cs`
- Create: `Models/VideoGeneration.cs`
- Create: `Models/VideoMerge.cs`
- Create: `Models/Prop.cs`

Each model maps to a SQLite table, matching the existing schema column-for-column.

Example for `Models/Drama.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("dramas")]
public class Drama
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("genre")]
    public string? Genre { get; set; }

    [Column("style")]
    public string? Style { get; set; } = "realistic";

    [Column("total_episodes")]
    public int? TotalEpisodes { get; set; } = 1;

    [Column("total_duration")]
    public int? TotalDuration { get; set; } = 0;

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("thumbnail")]
    public string? Thumbnail { get; set; }

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
```

All models follow the same pattern: `[Table("table_name")]`, `[Column("column_name")]`, `[Key]` with `Identity` generation for auto-increment.

### Task 1.3: Create DbContext

**Files:**
- Create: `Data/DramaDbContext.cs`
- Create: `Data/DbInitializer.cs`

- [ ] **Create DramaDbContext.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Models;

namespace WuShuangDrama.Data;

public class DramaDbContext : DbContext
{
    public DramaDbContext(DbContextOptions<DramaDbContext> options) : base(options) { }

    public DbSet<Drama> Dramas => Set<Drama>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<EpisodeCharacter> EpisodeCharacters => Set<EpisodeCharacter>();
    public DbSet<EpisodeScene> EpisodeScenes => Set<EpisodeScene>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Storyboard> Storyboards => Set<Storyboard>();
    public DbSet<StoryboardCharacters> StoryboardCharacters => Set<StoryboardCharacters>();
    public DbSet<AiServiceConfig> AiServiceConfigs => Set<AiServiceConfig>();
    public DbSet<AiVoice> AiVoices => Set<AiVoice>();
    public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();
    public DbSet<ImageGeneration> ImageGenerations => Set<ImageGeneration>();
    public DbSet<VideoGeneration> VideoGenerations => Set<VideoGeneration>();
    public DbSet<VideoMerge> VideoMerges => Set<VideoMerge>();
    public DbSet<Prop> Props => Set<Prop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite key for storyboard_characters
        modelBuilder.Entity<StoryboardCharacters>(entity =>
        {
            entity.HasKey(e => new { e.StoryboardId, e.CharacterId });
        });

        // Storyboard -> StoryboardCharacters cascade delete
        modelBuilder.Entity<StoryboardCharacters>()
            .HasOne<Storyboard>()
            .WithMany()
            .HasForeignKey(e => e.StoryboardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Create DbInitializer.cs**

```csharp
namespace WuShuangDrama.Data;

public static class DbInitializer
{
    public static void EnsureDbCreated(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DramaDbContext>();
        db.Database.EnsureCreated();
    }
}
```

- [ ] **Build to verify**

Run: `dotnet build`

---

## Phase 2: Light Theme CSS & Layout

### Task 2.1: Create global light theme stylesheet

**Files:**
- Create: `wwwroot/css/app.css`

This replaces the dark theme with a clean light/white theme. Write all CSS variables, base styles, card styles, button styles, form styles, modal styles, animation keyframes, and utility classes.

Key CSS variable definitions:

```css
:root {
  --bg-0: #ffffff;
  --bg-1: #f8f9fb;
  --bg-2: #f0f1f4;
  --bg-3: #e5e7eb;
  --bg-hover: #e8eaed;
  --bg-base: #f5f6f8;

  --text-0: #1a1d23;
  --text-1: #2c2f36;
  --text-2: #5a5f6b;
  --text-3: #8e93a0;

  --accent: #4c7dff;
  --accent-bg: #eef2ff;
  --accent-text: #3b6ee8;

  --success: #22c55e;
  --warning: #f59e0b;
  --error: #ef4444;

  --border: #e2e5eb;
  --border-strong: #c8ccd4;

  --radius: 10px;
  --radius-lg: 14px;
  --radius-xl: 18px;

  --shadow-xs: 0 1px 2px rgba(0,0,0,0.04);
  --shadow: 0 2px 8px rgba(0,0,0,0.06);
  --shadow-lg: 0 8px 24px rgba(0,0,0,0.08);
  --shadow-elevated: 0 20px 48px rgba(0,0,0,0.12);

  --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
  --font-sans: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  --font-mono: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
  --font-display: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
}
```

Include all component styles: `.card`, `.btn`, `.btn-primary`, `.btn-ghost`, `.input`, `.input-mono`, `.overlay`, `.modal`, `.field`, `.tag`, `.back-btn`, `.page`, `.page-head`, `.grid`, `.skeleton-card`, animation keyframes (`fadeUp`, `scaleIn`, `shimmer`), `.dialog-mask`, `.dialog`, progress bars, etc. Minimum 400 meaningful lines of CSS.

### Task 2.2: Update _Host.cshtml to reference app.css

**Files:**
- Modify: `Pages/_Host.cshtml`

Change `<link href="css/site.css" rel="stylesheet" />` to `<link href="css/app.css" rel="stylesheet" />`. Update html lang to "zh-CN". Add viewport meta.

### Task 2.3: Create NavMenu component

**Files:**
- Create: `Components/Layout/NavMenu.razor`
- Create: `Components/Layout/NavMenu.razor.css`

Top navigation bar with brand logo, project link, settings link, and film strip decoration. Matches the original `layouts/default.vue` in layout but with light theme colors.

### Task 2.4: Create MainLayout with NavMenu

**Files:**
- Create: `Components/Layout/MainLayout.razor`
- Create: `Components/Layout/MainLayout.razor.css`
- Modify: `App.razor` to use new layout

MainLayout renders NavMenu + `@Body`. Set `@rendermode InteractiveServer` on the Router.

### Task 2.5: Create shared components

**Files:**
- Create: `Components/Shared/BaseSelect.razor`
- Create: `Components/Shared/EmptyState.razor`

BaseSelect: a searchable select dropdown with label/value options, placeholder, keyboard navigation.

---

## Phase 3: Basic Pages (Index + DramaDetail)

### Task 3.1: Index page — project list

**Files:**
- Create: `Components/Pages/Index.razor`
- Create: `Components/Pages/Index.razor.css`

Page with:
- Grid of project cards (title, style, episode count, character count, scene count, progress, date)
- Loading skeleton state
- Empty state (create first project)
- Create project modal dialog (title, total episodes, style select)
- Delete project with confirmation
- Relative time display

Uses `DramaDbContext` injected via `@inject`. All data loading in `OnInitializedAsync` with error handling.

### Task 3.2: DramaDetail page — drama detail + episode list

**Files:**
- Create: `Components/Pages/DramaDetail.razor`
- Create: `Components/Pages/DramaDetail.razor.css`

Page with:
- Back button, drama title, style chip, meta stats
- Episode list (episode number, title, script status, duration)
- Add episode dialog with image/video/audio config selection
- Empty state for no episodes

Route: `/drama/{DramaId:int}`
Route: `/drama/{DramaId:int}/episode/{EpisodeNumber:int}` navigation to workbench

---

## Phase 4: Workbench (Core)

### Task 4.1: Workbench layout — studio topbar + sidebar pipeline

**Files:**
- Create: `Components/Pages/Workbench.razor`
- Create: `Components/Pages/Workbench.razor.css`

Layout with:
- Topbar: back button, drama title, episode chip, meta pills, refresh button, production button
- Left sidebar: pipeline navigation (11 steps organized by section)
- Right main content area (switches based on active step)
- Bottom: progress bar, step jump dots
- Loading and error states

Route: `/drama/{DramaId:int}/episode/{EpisodeNumber:int}`

Pipeline sections and steps:
1. 剧本 (Script): script_rewrite
2. 角色 (Characters): extract_characters, assign_voices, generate_voice_samples
3. 场景 (Scenes): extract_scenes
4. 分镜 (Storyboards): extract_storyboards, rate_scenes
5. 图片 (Images): generate_images
6. 视频 (Videos): generate_videos
7. 合成 (Compose): compose_shots
8. 导出 (Export): merge_episode, export_result

### Task 4.2: Pipeline status polling

The sidebar shows real-time pipeline progress. Implement a periodic timer (every 5-10 seconds) that fetches pipeline status from the server and updates step states (pending/processing/done/partial).

### Task 4.3: Script rewrite step

Content panel for script rewriting:
- Display original content (textarea)
- AI rewrite button → calls Agent → displays result
- Save button
- Loading state during agent execution

### Task 4.4: Character management step

Content panel for characters:
- Character list with name, role, appearance, voice style
- Generate image button per character
- Batch generate images button
- Voice sample generate + play button
- Loading states for each generation action

### Task 4.5: Scene management step

Content panel for scenes:
- Scene list (location, time, prompt)
- Generate scene image button
- Scene editing

### Task 4.6: Storyboard management step

Content panel for storyboards:
- Storyboard card list (shot number, title, scene, dialogue, image prompt, video prompt)
- Character assignment (multi-select)
- TTS generate per storyboard
- Image prompt editing
- Grid image generation (mode, rows, cols selection)
- Grid split after generation

### Task 4.7: Image generation management step

Content panel for generated images:
- Image grid for each storyboard
- Different frame types (first_frame, last_frame, composed)
- Regenerate button
- Loading/progress states

### Task 4.8: Video generation management step

Content panel for generated videos:
- Video list per storyboard
- Generate/regenerate video
- Play video preview
- Video generation status polling

### Task 4.9: Compose step

Content panel for shot composition:
- Compose individual or batch compose buttons
- Progress tracking per storyboard
- View composed video

### Task 4.10: Export step

Content panel for final export:
- Merge all composed videos into full episode
- Merge status + progress
- Watch/download final video
- Error handling

---

## Phase 5: Settings Page

### Task 5.1: Settings page — AI configs + agent configs

**Files:**
- Create: `Components/Pages/Settings.razor`
- Create: `Components/Pages/Settings.razor.css`

Left nav tabs: AI Service Configs, Agent Configs (advanced toggle).

AI Service Configs section:
- Huobao preset quick setup (one-click configure all 4 service types)
- Per service type (text/image/video/audio) config list
- Add/edit/delete config per service type
- Connection test button per config
- Model array editing

Agent Configs section:
- Agent list (5 types)
- Edit: model, temperature, max_tokens, max_iterations, system_prompt

---

## Phase 6: AI Agent Layer

### Task 6.1: Agent instructions + skills loader

**Files:**
- Create: `Agents/AgentInstructions.cs`
- Create: `Agents/Skills.cs`

AgentInstructions.cs: static class with all 5 default prompt strings (matching original Mastra agent prompts).

Skills.cs: loads SKILL.md files from the `skills/` directory, appends to agent instructions.

### Task 6.2: Agent factory

**Files:**
- Create: `Agents/AgentFactory.cs`

Factory that:
- Reads agent config from DB (agent_configs table)
- Creates appropriate AIChatAgent with instructions + tools
- Uses IChatClient from Microsoft.Extensions.AI
- Resolves model from text AI config

```csharp
public class AgentFactory
{
    private readonly DramaDbContext _db;
    private readonly AIService _aiService;
    private readonly ILogger<AgentFactory> _logger;

    public AIChatAgent CreateAgent(
        string agentType, 
        int episodeId, 
        int dramaId)
    {
        var config = _db.AgentConfigs
            .FirstOrDefault(c => c.AgentType == agentType && c.IsActive == true)
            ?? throw new InvalidOperationException($"No active config for {agentType}");

        var textConfig = _aiService.GetActiveConfig("text")
            ?? throw new InvalidOperationException("No active text AI config");

        var chatClient = CreateChatClient(textConfig);
        var instructions = BuildInstructions(config, agentType, episodeId, dramaId);
        var tools = BuildTools(agentType, episodeId, dramaId);
        
        var chatOptions = new ChatOptions
        {
            Temperature = (float?)config.Temperature ?? 0.7f,
            MaxOutputTokens = config.MaxTokens ?? 4096,
            Tools = tools.Cast<AITool>().ToList(),
        };

        return new AIChatAgent(chatClient, agentType, instructions, chatOptions);
    }

    private IChatClient CreateChatClient(AIConfig config)
    {
        // Use OpenAI-compatible client
        return new OpenAIClient(new HttpClient
        {
            BaseAddress = new Uri(GetTextProviderBaseUrl(config))
        }).AsChatClient(config.Model);
    }
}
```

### Task 6.3: Agent tools

**Files:**
- Create: `Agents/Tools/ScriptTools.cs`
- Create: `Agents/Tools/ExtractTools.cs`
- Create: `Agents/Tools/StoryboardTools.cs`
- Create: `Agents/Tools/VoiceTools.cs`
- Create: `Agents/Tools/GridPromptTools.cs`

Each tool class inherits from `AITool` and implements `InvokeAsync`:

```csharp
public class ReadEpisodeScript : AITool
{
    private readonly DramaDbContext _db;
    private readonly int _episodeId;

    public ReadEpisodeScript(DramaDbContext db, int episodeId) 
        : base("read_episode_script", "Read the episode script content")
    {
        _db = db;
        _episodeId = episodeId;
    }

    protected override async Task<object?> InvokeAsync(
        AIFunctionInvocationContext context,
        CancellationToken cancellationToken)
    {
        var episode = await _db.Episodes.FindAsync(new object[] { _episodeId }, cancellationToken);
        return episode?.Content ?? episode?.ScriptContent ?? "No content found";
    }
}
```

---

## Phase 7: Service Layer

### Task 7.1: AIService

**Files:**
- Create: `Services/AIService.cs`

Methods:
- `GetActiveConfig(string serviceType)` — returns AIConfig with provider, baseUrl, apiKey, model
- `GetConfigById(int id)` — individual config lookup
- `GetTextProviderBaseUrl(AIConfig)` — URL normalization per provider
- Connection test probing against various providers

### Task 7.2: ImageService

**Files:**
- Create: `Services/ImageService.cs`

Methods:
- `GenerateImage(ImageParams)` — insert record, call adapter, handle sync/async, poll
- `PollImageTask(int id)` — periodic polling loop
- `NormalizeReferenceImages(string[])` — local paths → base64 data URIs
- `HandleImageComplete(int id, string imageUrl)` — download + update DB + update related tables

### Task 7.3: VideoService

**Files:**
- Create: `Services/VideoService.cs`

Methods:
- `GenerateVideo(VideoParams)` — insert record, call adapter, poll
- `PollVideoTask(int id)` — 300 retries with 10s interval
- `NormalizeVideoReferenceUrl(string)` — local → base64
- `HandleVideoComplete(int id, string videoUrl)` — download + update DB

### Task 7.4: TTSService

**Files:**
- Create: `Services/TTSService.cs`

Methods:
- `GenerateTTS(TTSParams)` — call TTS adapter, save hex audio to file
- `GenerateVoiceSample(string name, string voiceId)` — sample text + TTS

### Task 7.5: ComposeService

**Files:**
- Create: `Services/ComposeService.cs`

Methods:
- `ComposeStoryboard(int storyboardId)` — FFmpeg compose (video + TTS audio + subtitle burn-in)
- `ParseDialogueForTTS(string?)` — extract speaker and pure text from dialogue
- `ToAbsPath(string)` — resolve relative paths to absolute

### Task 7.6: MergeService

**Files:**
- Create: `Services/MergeService.cs`

Methods:
- `MergeEpisodeVideos(int episodeId)` — FFmpeg concat all composed videos
- `GetVideoDuration(string)` — ffprobe duration extraction

### Task 7.7: GridService

**Files:**
- Create: `Services/GridService.cs`

Methods:
- `BuildGridPrompt(...)` — prompt building per mode (first_frame, first_last, multi_ref)
- `CollectGridReferenceAssets(...)` — collect reference images
- `SplitGridImage(string path, int rows, int cols)` — SkiaSharp image splitting
- `ExtractJsonCandidate(string)` — JSON extraction from agent text output

### Task 7.8: StorageService

**Files:**
- Create: `Services/StorageService.cs`

Methods:
- `DownloadFile(string url, string subDir)` — fetch URL → save to local
- `SaveUploadedFile(byte[] data, string subDir, string name)` — save local
- `ReadImageAsCompressedDataUrl(string path)` — SkiaSharp resize + base64
- `SaveBase64Image(string data, string mimeType, string subDir)` — save base64

---

## Phase 8: Adapter Layer (Multi-Vendor)

### Task 8.1: Adapter interfaces and models

**Files:**
- Create: `Adapters/IImageProvider.cs`
- Create: `Adapters/IVideoProvider.cs`
- Create: `Adapters/ITTSProvider.cs`
- Create: `Adapters/ProviderModels.cs`
- Create: `Adapters/AdapterRegistry.cs`

Define interfaces matching original TypeScript adapter pattern. ProviderModels.cs has shared DTOs (ProviderRequest, AIConfig, ImageGenResponse, etc.). AdapterRegistry.cs provides static lookup by provider name.

### Task 8.2: Image adapters (5 providers)

**Files:**
- Create: `Adapters/Image/OpenAIImageProvider.cs`
- Create: `Adapters/Image/GeminiImageProvider.cs`
- Create: `Adapters/Image/MiniMaxImageProvider.cs`
- Create: `Adapters/Image/VolcEngineImageProvider.cs`
- Create: `Adapters/Image/AliImageProvider.cs`

Each implements `IImageProvider` with provider-specific:
- `BuildGenerateRequest(AIConfig, ImageGenerationRecord)` — URL, method, headers, body
- `ParseGenerateResponse(JsonElement)` — isAsync, taskId, imageUrl
- `BuildPollRequest(AIConfig, string taskId)` — polling endpoint
- `ParsePollResponse(JsonElement)` — status, imageUrl, error
- `ExtractImageBase64(JsonElement)` — Gemini base64 handling

### Task 8.3: Video adapters (4 providers)

**Files:**
- Create: `Adapters/Video/MiniMaxVideoProvider.cs`
- Create: `Adapters/Video/VolcEngineVideoProvider.cs`
- Create: `Adapters/Video/ViduVideoProvider.cs`
- Create: `Adapters/Video/AliVideoProvider.cs`

Each implements `IVideoProvider` with provider-specific request/response handling.

### Task 8.4: TTS adapters (1 provider)

**Files:**
- Create: `Adapters/TTS/MiniMaxTTSProvider.cs`

Implements `ITTSProvider` for MiniMax TTS (hex audio response handling).

---

## Phase 9: File Upload & Static Files

### Task 9.1: Upload endpoint integration

In Blazor Server, file upload uses `InputFile` component. Wire it to StorageService:

```razor
<InputFile OnChange="HandleUpload" accept="image/*" />

@code {
    async Task HandleUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var stream = new MemoryStream();
        await file.OpenReadStream().CopyToAsync(stream);
        var path = await StorageService.SaveUploadedFile(
            stream.ToArray(), "uploads", file.Name);
    }
}
```

Serve static files from `data/static/` directory. Configure in Program.cs:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "data")),
    RequestPath = "/static"
});
```

---

## Phase 10: Skills Loading

### Task 10.1: Load SKILL.md files at runtime

**Files:**
- Modify: `Agents/Skills.cs`

Read SKILL.md files from `skills/` directory and append content to agent instructions:

```csharp
public static string LoadAgentSkills(string agentType)
{
    var skillPath = Path.Combine(Directory.GetCurrentDirectory(), 
        "skills", agentType, "SKILL.md");
    return File.Exists(skillPath) ? File.ReadAllText(skillPath) : "";
}
```

---

## API Route Mapping Reference

For converting from Hono routes to Blazor Server handlers. Since Blazor Server eliminates the REST API layer, all route logic moves into service methods called directly from Blazor components:

| Hono Route | Blazor Equivalent |
|------------|------------------|
| `GET /api/v1/dramas` | `Index.razor` → `_db.Dramas.Where(d => d.DeletedAt == null).ToListAsync()` |
| `POST /api/v1/dramas` | `Index.razor` create handler → `_db.Dramas.Add(drama)` |
| `GET /api/v1/dramas/:id` | `DramaDetail.razor` → `_db.Dramas.FindAsync(id)` |
| `PUT /api/v1/dramas/:id` | `DramaDetail.razor` update handler |
| `DELETE /api/v1/dramas/:id` | `Index.razor` delete handler (soft delete) |
| `GET /api/v1/dramas/stats` | `DramaDbContext` query |
| `GET/POST /api/v1/episodes` | `DramaDetail.razor` episode list + create |
| `GET /api/v1/episodes/:id/pipeline-status` | `PipelineStatusService` |
| `GET /api/v1/episodes/:id/storyboards` | `Workbench.razor` storyboard queries |
| `POST /api/v1/agent/:type/chat` | `AgentFactory.CreateAgent()` → `agent.GenerateAsync()` |
| `POST /api/v1/images` | `ImageService.GenerateImage()` |
| `POST /api/v1/videos` | `VideoService.GenerateVideo()` |
| `POST /api/v1/characters/:id/generate-voice-sample` | `TTSService.GenerateVoiceSample()` |
| `POST /api/v1/storyboards/:id/compose` | `ComposeService.ComposeStoryboard()` |
| `POST /api/v1/episodes/:id/merge` | `MergeService.MergeEpisodeVideos()` |
| `POST /api/v1/grid/prompt` | `GridService.BuildGridPrompt()` |
| `POST /api/v1/grid/generate` | `ImageService.GenerateImage()` |
| `POST /api/v1/grid/split` | `GridService.SplitGridImage()` |
| `POST /api/v1/upload/image` | `StorageService.SaveUploadedFile()` |
| `POST /api/v1/ai-configs/test` | `AIService.TestConnection()` |

---

## Build & Run Commands

```bash
# Build the project
cd WuShuangDrama
dotnet build

# Run development server
dotnet run

# Run without watching (production-like)
dotnet run --no-hot-reload

# The app runs on http://localhost:5000 by default
```
