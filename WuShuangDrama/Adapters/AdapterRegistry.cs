using WuShuangDrama.Adapters.Image;
using WuShuangDrama.Adapters.Video;
using WuShuangDrama.Adapters.TTS;

namespace WuShuangDrama.Adapters;

public static class AdapterRegistry
{
    private static readonly Dictionary<string, IImageProvider> _imageProviders = new();
    private static readonly Dictionary<string, IVideoProvider> _videoProviders = new();
    private static readonly Dictionary<string, ITTSProvider> _ttsProviders = new();

    static AdapterRegistry()
    {
        RegisterImage(new OpenAIImageProvider());
        RegisterImage(new GeminiImageProvider());
        RegisterImage(new MiniMaxImageProvider());
        RegisterImage(new VolcEngineImageProvider());
        RegisterImage(new AliImageProvider());

        RegisterVideo(new MiniMaxVideoProvider());
        RegisterVideo(new VolcEngineVideoProvider());
        RegisterVideo(new ViduVideoProvider());
        RegisterVideo(new AliVideoProvider());

        RegisterTTS(new MiniMaxTTSProvider());
    }

    public static void RegisterImage(IImageProvider provider) =>
        _imageProviders[provider.ProviderName] = provider;

    public static void RegisterVideo(IVideoProvider provider) =>
        _videoProviders[provider.ProviderName] = provider;

    public static void RegisterTTS(ITTSProvider provider) =>
        _ttsProviders[provider.ProviderName] = provider;

    public static IImageProvider? GetImageProvider(string name) =>
        _imageProviders.GetValueOrDefault(name);

    public static IVideoProvider? GetVideoProvider(string name) =>
        _videoProviders.GetValueOrDefault(name);

    public static ITTSProvider? GetTTSProvider(string name) =>
        _ttsProviders.GetValueOrDefault(name);

    public static IEnumerable<string> AllImageProviders => _imageProviders.Keys;
    public static IEnumerable<string> AllVideoProviders => _videoProviders.Keys;
    public static IEnumerable<string> AllTTSProviders => _ttsProviders.Keys;

    public static string NormalizeBaseUrl(string provider, string baseUrl)
    {
        return (provider, baseUrl) switch
        {
            ("gemini", _) when !baseUrl.Contains("generateContent") =>
                baseUrl.TrimEnd('/') + "/models",
            ("minimax", _) when !baseUrl.EndsWith("/v1") =>
                baseUrl.TrimEnd('/') + "/v1",
            _ => baseUrl.TrimEnd('/')
        };
    }
}
