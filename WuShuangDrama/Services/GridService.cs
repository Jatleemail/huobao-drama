using System.Text.Json;
using SkiaSharp;

namespace WuShuangDrama.Services;

public class GridService
{
    private readonly ILogger<GridService> _logger;

    public GridService(ILogger<GridService> logger)
    {
        _logger = logger;
    }

    public string BuildGridPrompt(
        string mode,
        string storyboardPrompt,
        string? characterDescription,
        List<string>? referenceImages,
        int rows = 2,
        int cols = 2)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Mode: {mode}");
        sb.AppendLine($"Rows: {rows}, Cols: {cols}");
        sb.AppendLine();
        sb.AppendLine("Scene description:");
        sb.AppendLine(storyboardPrompt);

        if (!string.IsNullOrEmpty(characterDescription))
        {
            sb.AppendLine();
            sb.AppendLine("Character:");
            sb.AppendLine(characterDescription);
        }

        if (referenceImages != null && referenceImages.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Reference images:");
            foreach (var img in referenceImages)
                sb.AppendLine($"- {img}");
        }

        sb.AppendLine();
        sb.AppendLine(mode switch
        {
            "first_frame" => "Generate the first frame of each shot in a grid layout.",
            "first_last" => "Generate first and last frames for each shot arranged in a grid.",
            "multi_ref" => "Generate multiple variations using reference images in a grid.",
            _ => "Generate a grid of images based on the storyboard."
        });

        return sb.ToString();
    }

    public async Task<List<string>> SplitGridImage(string imagePath, int rows, int cols)
    {
        var result = new List<string>();

        try
        {
            using var input = File.OpenRead(imagePath);
            using var bitmap = SKBitmap.Decode(input);
            if (bitmap == null) return result;

            var cellWidth = bitmap.Width / cols;
            var cellHeight = bitmap.Height / rows;

            var dir = Path.GetDirectoryName(imagePath)!;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(imagePath);
            var ext = Path.GetExtension(imagePath);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cellRect = new SKRectI(c * cellWidth, r * cellHeight, (c + 1) * cellWidth, (r + 1) * cellHeight);
                    using var cell = new SKBitmap(cellWidth, cellHeight);
                    bitmap.ExtractSubset(cell, cellRect);

                    var cellPath = Path.Combine(dir, $"{nameWithoutExt}_cell_{r}_{c}{ext}");
                    using var image = SKImage.FromBitmap(cell);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    await using var fs = File.Create(cellPath);
                    data.SaveTo(fs);

                    result.Add(cellPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to split grid image: {Path}", imagePath);
        }

        return result;
    }

    public static string ExtractJsonCandidate(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        start = text.IndexOf('[');
        end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }
}
