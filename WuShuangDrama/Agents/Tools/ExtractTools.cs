using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Agents.Tools;

public class SaveExtractedCharacters
{
    private readonly DramaDbContext _db;
    private readonly int _dramaId;

    public SaveExtractedCharacters(DramaDbContext db, int dramaId)
    {
        _db = db;
        _dramaId = dramaId;
    }

    public async Task<string> InvokeAsync(string charactersJson)
    {
        var episode = await _db.Episodes
            .Where(e => e.DramaId == _dramaId)
            .OrderBy(e => e.EpisodeNumber)
            .FirstOrDefaultAsync();

        if (episode == null) return "No episode found";

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(charactersJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("characters", out var chars))
            {
                foreach (var c in chars.EnumerateArray())
                {
                    var character = new Character
                    {
                        DramaId = _dramaId,
                        Name = c.GetProperty("name").GetString() ?? "",
                        Role = c.GetProperty("role").GetString(),
                        Appearance = c.TryGetProperty("appearance", out var app) ? app.GetString() : null,
                        Personality = c.TryGetProperty("personality", out var per) ? per.GetString() : null,
                        VoiceStyle = c.TryGetProperty("voiceStyle", out var vs) ? vs.GetString() : null,
                        CreatedAt = DateTime.UtcNow.ToString("o"),
                        UpdatedAt = DateTime.UtcNow.ToString("o"),
                    };
                    _db.Characters.Add(character);
                }
            }

            if (root.TryGetProperty("scenes", out var scenes))
            {
                foreach (var s in scenes.EnumerateArray())
                {
                    var scene = new Scene
                    {
                        DramaId = _dramaId,
                        EpisodeId = episode.Id,
                        Location = s.GetProperty("location").GetString() ?? "",
                        Time = s.TryGetProperty("time", out var t) ? t.GetString() ?? "" : "",
                        Prompt = s.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow.ToString("o"),
                        UpdatedAt = DateTime.UtcNow.ToString("o"),
                    };
                    _db.Scenes.Add(scene);
                }
            }

            await _db.SaveChangesAsync();
            return $"Saved {chars.EnumerateArray().Count()} characters and {scenes.EnumerateArray().Count()} scenes";
        }
        catch (Exception ex)
        {
            return $"Error saving extracted data: {ex.Message}";
        }
    }
}
