using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Services;

public class MergeService
{
    private readonly DramaDbContext _db;
    private readonly ILogger<MergeService> _logger;

    public MergeService(DramaDbContext db, ILogger<MergeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string?> MergeEpisodeVideos(int episodeId, CancellationToken ct = default)
    {
        var storyboards = await _db.Storyboards
            .Where(s => s.EpisodeId == episodeId && !string.IsNullOrEmpty(s.ComposedVideoUrl))
            .OrderBy(s => s.StoryboardNumber)
            .ToListAsync(ct);

        if (storyboards.Count == 0)
        {
            _logger.LogWarning("No composed videos found for episode {EpisodeId}", episodeId);
            return null;
        }

        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "data");

        var fileList = Path.Combine(dataDir, $"merge_list_{episodeId}.txt");
        var lines = storyboards
            .Select(s => ToAbsPath(s.ComposedVideoUrl!))
            .Select(p => $"file '{p.Replace("'", "'\\''")}'")
            .ToList();
        await File.WriteAllLinesAsync(fileList, lines, ct);

        var outputPath = Path.Combine(dataDir, "static", "merged", $"episode_{episodeId}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp4");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -f concat -safe 0 -i \"{fileList}\" -c copy \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("FFmpeg merge failed: {Error}", error);
                return null;
            }

            var duration = await GetVideoDuration(outputPath, ct);

            var merge = new VideoMerge
            {
                EpisodeId = episodeId,
                Title = $"Episode {episodeId} Merge",
                Status = "done",
                MergedUrl = $"/static/merged/{Path.GetFileName(outputPath)}",
                Duration = duration,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CompletedAt = DateTime.UtcNow.ToString("o"),
            };
            _db.VideoMerges.Add(merge);

            var episode = await _db.Episodes.FindAsync(new object[] { episodeId }, ct);
            if (episode != null)
            {
                episode.VideoUrl = merge.MergedUrl;
                episode.Status = "done";
                episode.UpdatedAt = DateTime.UtcNow.ToString("o");
            }

            await _db.SaveChangesAsync(ct);
            return merge.MergedUrl;
        }
        finally
        {
            if (File.Exists(fileList)) File.Delete(fileList);
        }
    }

    public async Task<int> GetVideoDuration(string filePath, CancellationToken ct = default)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (double.TryParse(output.Trim(), out var seconds))
                return (int)Math.Ceiling(seconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get video duration: {Path}", filePath);
        }

        return 0;
    }

    private string ToAbsPath(string relativeUrl)
    {
        var clean = relativeUrl.TrimStart('/');
        if (clean.StartsWith("static/"))
            clean = clean["static/".Length..];
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "data");
        return Path.GetFullPath(Path.Combine(baseDir, clean));
    }
}
