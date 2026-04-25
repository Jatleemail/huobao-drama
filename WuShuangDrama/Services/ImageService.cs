using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Adapters;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Services;

public class ImageService
{
    private readonly DramaDbContext _db;
    private readonly AIService _aiService;
    private readonly StorageService _storage;
    private readonly ILogger<ImageService> _logger;

    public ImageService(DramaDbContext db, AIService aiService, StorageService storage, ILogger<ImageService> logger)
    {
        _db = db;
        _aiService = aiService;
        _storage = storage;
        _logger = logger;
    }

    public async Task<ImageGeneration> GenerateImage(int storyboardId, string imageType, string? frameType = null, CancellationToken ct = default)
    {
        var storyboard = await _db.Storyboards.FindAsync(new object[] { storyboardId }, ct)
            ?? throw new InvalidOperationException($"Storyboard {storyboardId} not found");

        var config = await _aiService.GetActiveConfig("image")
            ?? throw new InvalidOperationException("No active image AI config");

        var provider = AdapterRegistry.GetImageProvider(config.Provider);
        if (provider == null)
            throw new InvalidOperationException($"No image provider for {config.Provider}");

        var record = new ImageGeneration
        {
            StoryboardId = storyboardId,
            DramaId = storyboard.EpisodeId != null
                ? await _db.Episodes.Where(e => e.Id == storyboard.EpisodeId).Select(e => e.DramaId).FirstOrDefaultAsync(ct) : null,
            ImageType = imageType,
            FrameType = frameType,
            Provider = config.Provider,
            Prompt = frameType switch
            {
                "first_frame" => storyboard.ImagePrompt,
                "last_frame" => storyboard.VideoPrompt,
                _ => storyboard.ImagePrompt
            },
            Model = config.Model,
            Status = "pending",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };
        _db.ImageGenerations.Add(record);
        await _db.SaveChangesAsync(ct);

        var request = new ImageRequest
        {
            Prompt = record.Prompt ?? "",
            NegativePrompt = record.NegativePrompt,
            Model = record.Model,
            Size = record.Size,
            Quality = record.Quality,
            Style = record.Style,
            Steps = record.Steps,
            CfgScale = record.CfgScale,
            Seed = record.Seed,
        };

        try
        {
            var response = await provider.GenerateImageAsync(config, request, ct);
            if (response.IsAsync)
            {
                record.TaskId = response.TaskId;
                record.Status = "processing";
                await _db.SaveChangesAsync(ct);
                _ = PollImageTask(record.Id, ct);
            }
            else if (response.ImageUrl != null)
            {
                await HandleImageComplete(record.Id, response.ImageUrl, ct);
            }
            else
            {
                record.Status = "error";
                record.ErrorMsg = response.Error ?? "No image URL returned";
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

    public async Task PollImageTask(int id, CancellationToken ct = default)
    {
        var record = await _db.ImageGenerations.FindAsync(new object[] { id }, ct);
        if (record?.TaskId == null) return;

        var config = await _aiService.GetActiveConfig("image");
        if (config == null) return;

        var provider = AdapterRegistry.GetImageProvider(config.Provider);
        if (provider == null) return;

        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(5000, ct);
            try
            {
                var result = await provider.PollImageAsync(config, record.TaskId, ct);
                if (result.Status == "done" && result.ResultUrl != null)
                {
                    await HandleImageComplete(id, result.ResultUrl, ct);
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
                _logger.LogWarning(ex, "Poll image task {Id} attempt {Attempt}", id, i);
            }
        }
        record.Status = "error";
        record.ErrorMsg = "Polling timed out";
        await _db.SaveChangesAsync(ct);
    }

    public async Task HandleImageComplete(int id, string imageUrl, CancellationToken ct = default)
    {
        var record = await _db.ImageGenerations.FindAsync(new object[] { id }, ct);
        if (record == null) return;

        string localUrl;
        if (imageUrl.StartsWith("http"))
        {
            localUrl = await _storage.DownloadFile(imageUrl, "images");
        }
        else if (imageUrl.StartsWith("data:"))
        {
            var parts = imageUrl.Split(';')[0];
            var mime = parts.Split(':')[1];
            localUrl = await _storage.SaveBase64Image(imageUrl, mime, "images");
        }
        else
        {
            localUrl = imageUrl;
        }

        record.ImageUrl = localUrl;
        record.LocalPath = _storage.GetFullPath(localUrl);
        record.Status = "done";
        record.CompletedAt = DateTime.UtcNow.ToString("o");
        record.UpdatedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync(ct);

        var sb = await _db.Storyboards.FindAsync(new object[] { record.StoryboardId }, ct);
        if (sb != null)
        {
            if (record.FrameType == "first_frame" || record.ImageType == "composed")
                sb.ComposedImage = localUrl;
            else if (record.FrameType == "first_frame")
                sb.FirstFrameImage = localUrl;
            else if (record.FrameType == "last_frame")
                sb.LastFrameImage = localUrl;
            sb.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _db.SaveChangesAsync(ct);
        }
    }
}
