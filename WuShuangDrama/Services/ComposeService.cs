using System.Diagnostics;

namespace WuShuangDrama.Services;

public class ComposeService
{
    private readonly ILogger<ComposeService> _logger;

    public ComposeService(ILogger<ComposeService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ComposeStoryboard(
        string videoPath,
        string audioPath,
        string outputPath,
        string? subtitlePath = null,
        CancellationToken ct = default)
    {
        try
        {
            var args = $"-y -i \"{videoPath}\"";

            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                args += $" -i \"{audioPath}\"";

            args += " -c:v libx264 -preset medium -crf 23";

            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                args += " -c:a aac -b:a 128k -shortest";
            else
                args += " -an";

            if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
                args += $" -vf \"subtitles='{subtitlePath.Replace("'", "'\\''")}'\"";
            else
                args += " -vf \"scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2\"";

            args += $" \"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
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
                _logger.LogError("FFmpeg compose failed: {Error}", error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compose storyboard failed");
            return false;
        }
    }

    public string ToAbsPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "data");
        return Path.GetFullPath(Path.Combine(baseDir, relativePath.TrimStart('/').TrimStart("static/".ToCharArray())));
    }
}
