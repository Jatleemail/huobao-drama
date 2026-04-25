using System.Text.Json;

namespace WuShuangDrama.Adapters;

public interface IImageProvider
{
    string ProviderName { get; }
    Task<ImageGenResponse> GenerateImageAsync(AIConfig config, ImageRequest request, CancellationToken ct = default);
    Task<PollResult> PollImageAsync(AIConfig config, string taskId, CancellationToken ct = default);
    ImageGenResponse ParseGenerateResponse(JsonElement json);
    PollResult ParsePollResponse(JsonElement json);
}
