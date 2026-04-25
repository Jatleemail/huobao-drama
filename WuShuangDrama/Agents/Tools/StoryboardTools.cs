using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Agents.Tools;

public class SaveExtractedStoryboards
{
    private readonly DramaDbContext _db;
    private readonly int _episodeId;

    public SaveExtractedStoryboards(DramaDbContext db, int episodeId)
    {
        _db = db;
        _episodeId = episodeId;
    }

    public async Task<string> InvokeAsync(string storyboardsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(storyboardsJson);
            var root = doc.RootElement;

            List<System.Text.Json.JsonElement> items;
            if (root.ValueKind == JsonValueKind.Array)
            {
                items = root.EnumerateArray().ToList();
            }
            else if (root.TryGetProperty("storyboards", out var sbArr))
            {
                items = sbArr.EnumerateArray().ToList();
            }
            else
            {
                items = new List<System.Text.Json.JsonElement>();
            }

            int count = 0;
            foreach (var item in items)
            {
                var sb = new Storyboard
                {
                    EpisodeId = _episodeId,
                    StoryboardNumber = item.TryGetProperty("storyboardNumber", out var sn) ? sn.GetInt32() : count + 1,
                    Title = item.TryGetProperty("title", out var t) ? t.GetString() : null,
                    Location = item.TryGetProperty("location", out var l) ? l.GetString() : null,
                    Time = item.TryGetProperty("time", out var ti) ? ti.GetString() : null,
                    ShotType = item.TryGetProperty("shotType", out var st) ? st.GetString() : null,
                    Angle = item.TryGetProperty("angle", out var a) ? a.GetString() : null,
                    Movement = item.TryGetProperty("movement", out var m) ? m.GetString() : null,
                    Action = item.TryGetProperty("action", out var ac) ? ac.GetString() : null,
                    Atmosphere = item.TryGetProperty("atmosphere", out var at) ? at.GetString() : null,
                    ImagePrompt = item.TryGetProperty("imagePrompt", out var ip) ? ip.GetString() : null,
                    VideoPrompt = item.TryGetProperty("videoPrompt", out var vp) ? vp.GetString() : null,
                    BgmPrompt = item.TryGetProperty("bgmPrompt", out var bp) ? bp.GetString() : null,
                    SoundEffect = item.TryGetProperty("soundEffect", out var se) ? se.GetString() : null,
                    Dialogue = item.TryGetProperty("dialogue", out var d) ? d.GetString() : null,
                    Duration = item.TryGetProperty("duration", out var du) ? du.GetInt32() : 0,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = DateTime.UtcNow.ToString("o"),
                };
                _db.Storyboards.Add(sb);
                count++;
            }

            await _db.SaveChangesAsync();
            return $"Saved {count} storyboards";
        }
        catch (Exception ex)
        {
            return $"Error saving storyboards: {ex.Message}";
        }
    }
}

public class ReadStoryboards
{
    private readonly DramaDbContext _db;
    private readonly int _episodeId;

    public ReadStoryboards(DramaDbContext db, int episodeId)
    {
        _db = db;
        _episodeId = episodeId;
    }

    public async Task<string> InvokeAsync()
    {
        var storyboards = await _db.Storyboards
            .Where(s => s.EpisodeId == _episodeId)
            .OrderBy(s => s.StoryboardNumber)
            .ToListAsync();

        return JsonSerializer.Serialize(storyboards, new JsonSerializerOptions { WriteIndented = true });
    }
}
