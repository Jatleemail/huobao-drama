using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Data;

namespace WuShuangDrama.Agents.Tools;

public class ReadStoryboardForGrid
{
    private readonly DramaDbContext _db;
    private readonly int _storyboardId;

    public ReadStoryboardForGrid(DramaDbContext db, int storyboardId)
    {
        _db = db;
        _storyboardId = storyboardId;
    }

    public async Task<string> InvokeAsync()
    {
        var sb = await _db.Storyboards.FindAsync(_storyboardId);
        if (sb == null) return "Storyboard not found";

        var characterNames = await _db.StoryboardCharacters
            .Where(sc => sc.StoryboardId == _storyboardId)
            .Join(_db.Characters, sc => sc.CharacterId, c => c.Id, (sc, c) => c.Name)
            .ToListAsync();

        var parts = new List<string>
        {
            $"Title: {sb.Title}",
            $"Location: {sb.Location}",
            $"Time: {sb.Time}",
            $"Image Prompt: {sb.ImagePrompt}",
            $"Video Prompt: {sb.VideoPrompt}",
            $"Atmosphere: {sb.Atmosphere}",
            $"Characters: {string.Join(", ", characterNames)}",
            $"Dialogue: {sb.Dialogue}",
        };

        return string.Join("\n", parts.Where(p => p != null));
    }
}
