using System.Text.Json;

namespace WuShuangDrama.Adapters;

public interface IVideoProvider
{
    string ProviderName { get; }
    Task<VideoGenResponse> GenerateVideoAsync(AIConfig config, VideoRequest request, CancellationToken ct = default);
    Task<PollResult> PollVideoAsync(AIConfig config, string taskId, CancellationToken ct = default);
    VideoGenResponse ParseGenerateResponse(JsonElement json);
    PollResult ParsePollResponse(JsonElement json);
}
