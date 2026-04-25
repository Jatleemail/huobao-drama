using SkiaSharp;

namespace WuShuangDrama.Services;

public class StorageService
{
    private readonly string _staticDir;
    private readonly ILogger<StorageService> _logger;

    public StorageService(ILogger<StorageService> logger)
    {
        _logger = logger;
        _staticDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "data", "static");
        Directory.CreateDirectory(_staticDir);
    }

    public async Task<string> DownloadFile(string url, string subDir)
    {
        var dir = Path.Combine(_staticDir, subDir);
        Directory.CreateDirectory(dir);

        using var http = new HttpClient();
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var ext = Path.GetExtension(url.Split('?')[0]) ?? ".bin";
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        await using var fs = File.Create(filePath);
        await response.Content.CopyToAsync(fs);

        var relativePath = Path.Combine("static", subDir, fileName).Replace('\\', '/');
        return $"/{relativePath}";
    }

    public async Task<string> SaveUploadedFile(byte[] data, string subDir, string name)
    {
        var dir = Path.Combine(_staticDir, subDir);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}_{name}";
        var filePath = Path.Combine(dir, fileName);

        await File.WriteAllBytesAsync(filePath, data);

        var relativePath = Path.Combine("static", subDir, fileName).Replace('\\', '/');
        return $"/{relativePath}";
    }

    public string? ReadImageAsCompressedDataUrl(string path, int maxWidth = 512)
    {
        try
        {
            var fullPath = Path.Combine(_staticDir, path.TrimStart('/').TrimStart("static/".ToCharArray()));
            if (!File.Exists(fullPath)) return null;

            using var input = File.OpenRead(fullPath);
            using var original = SKBitmap.Decode(input);
            if (original == null) return null;

            var scale = Math.Min(1.0, (double)maxWidth / original.Width);
            var newWidth = (int)(original.Width * scale);
            var newHeight = (int)(original.Height * scale);

            using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
            if (resized == null) return null;

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            var base64 = Convert.ToBase64String(data.ToArray());
            return $"data:image/jpeg;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read image as data URL: {Path}", path);
            return null;
        }
    }

    public async Task<string> SaveBase64Image(string data, string mimeType, string subDir)
    {
        var base64Data = data;
        if (base64Data.Contains(','))
            base64Data = base64Data[(base64Data.IndexOf(',') + 1)..];

        var bytes = Convert.FromBase64String(base64Data);
        var ext = mimeType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        var dir = Path.Combine(_staticDir, subDir);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        var relativePath = Path.Combine("static", subDir, fileName).Replace('\\', '/');
        return $"/{relativePath}";
    }

    public string GetFullPath(string relativeUrl)
    {
        var cleanPath = relativeUrl.TrimStart('/');
        if (cleanPath.StartsWith("static/"))
            cleanPath = cleanPath["static/".Length..];
        return Path.Combine(_staticDir, cleanPath);
    }

    public bool FileExists(string relativeUrl)
    {
        return File.Exists(GetFullPath(relativeUrl));
    }
}
