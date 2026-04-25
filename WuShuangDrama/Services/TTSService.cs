using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Adapters;
using WuShuangDrama.Data;
using WuShuangDrama.Models;

namespace WuShuangDrama.Services;

public class TTSService
{
    private readonly DramaDbContext _db;
    private readonly AIService _aiService;
    private readonly StorageService _storage;
    private readonly ILogger<TTSService> _logger;

    public TTSService(DramaDbContext db, AIService aiService, StorageService storage, ILogger<TTSService> logger)
    {
        _db = db;
        _aiService = aiService;
        _storage = storage;
        _logger = logger;
    }

    public async Task<string?> GenerateTTS(int storyboardId, CancellationToken ct = default)
    {
        var storyboard = await _db.Storyboards.FindAsync(new object[] { storyboardId }, ct);
        if (storyboard?.Dialogue == null) return null;

        var config = await _aiService.GetActiveConfig("audio");
        if (config == null) return null;

        var provider = AdapterRegistry.GetTTSProvider(config.Provider);
        if (provider == null) return null;

        var (speaker, text) = ParseDialogue(storyboard.Dialogue);

        var character = await _db.Characters
            .Where(c => c.Name == speaker && c.DramaId == storyboard.EpisodeId)
            .FirstOrDefaultAsync(ct);

        var request = new TTSRequest
        {
            Text = text,
            VoiceId = character?.VoiceStyle,
            Model = config.Model,
        };

        try
        {
            var response = await provider.GenerateTTSAsync(config, request, ct);
            if (response.AudioData != null)
            {
                var url = await _storage.SaveUploadedFile(response.AudioData, "audio", $"tts_{storyboardId}.mp3");
                storyboard.TtsAudioUrl = url;
                storyboard.UpdatedAt = DateTime.UtcNow.ToString("o");
                await _db.SaveChangesAsync(ct);
                return url;
            }
            if (response.AudioBase64 != null)
            {
                var bytes = Convert.FromBase64String(response.AudioBase64);
                var url = await _storage.SaveUploadedFile(bytes, "audio", $"tts_{storyboardId}.mp3");
                storyboard.TtsAudioUrl = url;
                storyboard.UpdatedAt = DateTime.UtcNow.ToString("o");
                await _db.SaveChangesAsync(ct);
                return url;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS generation failed for storyboard {Id}", storyboardId);
        }

        return null;
    }

    public async Task<string?> GenerateVoiceSample(string name, string voiceId, CancellationToken ct = default)
    {
        var config = await _aiService.GetActiveConfig("audio");
        if (config == null) return null;

        var provider = AdapterRegistry.GetTTSProvider(config.Provider);
        if (provider == null) return null;

        var request = new TTSRequest
        {
            Text = $"你好，我是{name}。很高兴认识你。",
            VoiceId = voiceId,
        };

        try
        {
            var response = await provider.GenerateTTSAsync(config, request, ct);
            if (response.AudioData != null)
                return await _storage.SaveUploadedFile(response.AudioData, "audio", $"voice_sample_{name}.mp3");
            if (response.AudioBase64 != null)
            {
                var bytes = Convert.FromBase64String(response.AudioBase64);
                return await _storage.SaveUploadedFile(bytes, "audio", $"voice_sample_{name}.mp3");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice sample generation failed for {Name}", name);
        }

        return null;
    }

    public static (string? Speaker, string Text) ParseDialogue(string? dialogue)
    {
        if (string.IsNullOrEmpty(dialogue)) return (null, "");

        var match = System.Text.RegularExpressions.Regex.Match(dialogue, @"^(.+?)[:：](.+)$");
        if (match.Success)
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());

        return (null, dialogue);
    }
}
