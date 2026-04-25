using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Data;

namespace WuShuangDrama.Agents.Tools;

public class ReadEpisodeScript
{
    private readonly DramaDbContext _db;
    private readonly int _episodeId;

    public ReadEpisodeScript(DramaDbContext db, int episodeId)
    {
        _db = db;
        _episodeId = episodeId;
    }

    public async Task<string> InvokeAsync()
    {
        var episode = await _db.Episodes.FindAsync(_episodeId);
        return episode?.Content ?? episode?.ScriptContent ?? "No content found";
    }
}

public class UpdateEpisodeScript
{
    private readonly DramaDbContext _db;
    private readonly int _episodeId;

    public UpdateEpisodeScript(DramaDbContext db, int episodeId)
    {
        _db = db;
        _episodeId = episodeId;
    }

    public async Task<string> InvokeAsync(string scriptContent)
    {
        var episode = await _db.Episodes.FindAsync(_episodeId);
        if (episode == null) return "Episode not found";

        episode.ScriptContent = scriptContent;
        episode.UpdatedAt = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
        return "Script updated successfully";
    }
}
