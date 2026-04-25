using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Agents.Tools;

public class ReadAvailableVoices
{
    private readonly DramaDbContext _db;

    public ReadAvailableVoices(DramaDbContext db)
    {
        _db = db;
    }

    public async Task<string> InvokeAsync()
    {
        var voices = await _db.AiVoices.ToListAsync();
        return JsonSerializer.Serialize(voices, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class AssignVoiceToCharacter
{
    private readonly DramaDbContext _db;
    private readonly int _dramaId;

    public AssignVoiceToCharacter(DramaDbContext db, int dramaId)
    {
        _db = db;
        _dramaId = dramaId;
    }

    public async Task<string> InvokeAsync(string assignmentsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(assignmentsJson);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var characterName = item.GetProperty("characterName").GetString();
                var voiceId = item.GetProperty("voiceId").GetString();
                var voiceStyle = item.GetProperty("voiceStyle").GetString();

                var character = await _db.Characters
                    .Where(c => c.DramaId == _dramaId && c.Name == characterName)
                    .FirstOrDefaultAsync();

                if (character != null)
                {
                    character.VoiceStyle = voiceStyle;
                    character.VoiceProvider = voiceId;
                    character.UpdatedAt = DateTime.UtcNow.ToString("o");
                }
            }

            await _db.SaveChangesAsync();
            return "Voice assignments saved successfully";
        }
        catch (Exception ex)
        {
            return $"Error saving voice assignments: {ex.Message}";
        }
    }
}
