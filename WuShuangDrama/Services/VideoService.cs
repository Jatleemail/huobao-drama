using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Adapters;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Services;

public class VideoService
{
    private readonly DramaDbContext _db;
    private readonly AIService _aiService;
    private readonly StorageService _storage;
    private readonly ILogger<VideoService> _logger;

    public VideoService(DramaDbContext db, AIService aiService, StorageService storage, ILogger<VideoService> logger)
    {
        _db = db;
        _aiService = aiService;
        _storage = storage;
        _logger = logger;
    }

    public async Task<VideoGeneration> GenerateVideo(int storyboardId, CancellationToken ct = default)
    {
        var storyboard = await _db.Storyboards.FindAsync(new object[] { storyboardId }, ct)
            ?? throw new InvalidOperationException($"Storyboard {storyboardId} not found");

        var config = await _aiService.GetActiveConfig("video")
            ?? throw new InvalidOperationException("No active video AI config");

        var provider = AdapterRegistry.GetVideoProvider(config.Provider);
        if (provider == null)
            throw new InvalidOperationException($"No video provider for {config.Provider}");

        var record = new VideoGeneration
        {
            StoryboardId = storyboardId,
            Provider = config.Provider,
            Prompt = storyboard.VideoPrompt,
            Model = config.Model,
            ImageUrl = storyboard.ComposedImage ?? storyboard.FirstFrameImage,
            Status = "pending",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };
        _db.VideoGenerations.Add(record);
        await _db.SaveChangesAsync(ct);

        var request = new VideoRequest
        {
            Prompt = record.Prompt ?? "",
            Model = record.Model,
            ImageUrl = record.ImageUrl,
            FirstFrameUrl = storyboard.FirstFrameImage,
            LastFrameUrl = storyboard.LastFrameImage,
            Duration = record.Duration,
            Fps = record.Fps,
            AspectRatio = record.AspectRatio,
            Style = record.Style,
            Seed = record.Seed,
        };

        try
        {
            var response = await provider.GenerateVideoAsync(config, request, ct);
            if (response.IsAsync)
            {
                record.TaskId = response.TaskId;
                record.Status = "processing";
                await _db.SaveChangesAsync(ct);
                _ = PollVideoTask(record.Id, ct);
            }
            else if (response.VideoUrl != null)
            {
                await HandleVideoComplete(record.Id, response.VideoUrl, ct);
            }
            else
            {
                record.Status = "error";
                record.ErrorMsg = response.Error ?? "No video URL returned";
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            record.Status = "error";
            record.ErrorMsg = ex.Message;
            await _db.SaveChangesAsync(ct);
        }

        return record;
    }

    public async Task PollVideoTask(int id, CancellationToken ct = default)
    {
        var record = await _db.VideoGenerations.FindAsync(new object[] { id }, ct);
        if (record?.TaskId == null) return;

        var config = await _aiService.GetActiveConfig("video");
        if (config == null) return;

        var provider = AdapterRegistry.GetVideoProvider(config.Provider);
        if (provider == null) return;

        for (int i = 0; i < 300; i++)
        {
            await Task.Delay(10000, ct);
            try
            {
                var result = await provider.PollVideoAsync(config, record.TaskId, ct);
                if (result.Status == "done" && result.ResultUrl != null)
                {
                    await HandleVideoComplete(id, result.ResultUrl, ct);
                    return;
                }
                if (result.Status == "error")
                {
                    record.Status = "error";
                    record.ErrorMsg = result.Error;
                    await _db.SaveChangesAsync(ct);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Poll video task {Id} attempt {Attempt}", id, i);
            }
        }
        record.Status = "error";
        record.ErrorMsg = "Polling timed out";
        await _db.SaveChangesAsync(ct);
    }

    public async Task HandleVideoComplete(int id, string videoUrl, CancellationToken ct = default)
    {
        var record = await _db.VideoGenerations.FindAsync(new object[] { id }, ct);
        if (record == null) return;

        string localUrl;
        if (videoUrl.StartsWith("http"))
            localUrl = await _storage.DownloadFile(videoUrl, "videos");
        else
            localUrl = videoUrl;

        record.VideoUrl = localUrl;
        record.LocalPath = _storage.GetFullPath(localUrl);
        record.Status = "done";
        record.CompletedAt = DateTime.UtcNow.ToString("o");
        record.UpdatedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync(ct);

        var sb = await _db.Storyboards.FindAsync(new object[] { record.StoryboardId }, ct);
        if (sb != null)
        {
            sb.VideoUrl = localUrl;
            sb.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _db.SaveChangesAsync(ct);
        }
    }
}
