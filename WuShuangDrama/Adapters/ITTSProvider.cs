namespace WuShuangDrama.Adapters;

public interface ITTSProvider
{
    string ProviderName { get; }
    Task<TTSResponse> GenerateTTSAsync(AIConfig config, TTSRequest request, CancellationToken ct = default);
}
