using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Adapters;
using WuShuangDrama.Data;

namespace WuShuangDrama.Services;

public class AIService
{
    private readonly DramaDbContext _db;
    private readonly ILogger<AIService> _logger;

    public AIService(DramaDbContext db, ILogger<AIService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AIConfig?> GetActiveConfig(string serviceType)
    {
        return await _db.AiServiceConfigs
            .Where(c => c.ServiceType == serviceType && c.IsActive == true)
            .OrderBy(c => c.Priority)
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result == null ? null : new AIConfig
            {
                Provider = t.Result.Provider ?? "openai",
                BaseUrl = t.Result.BaseUrl,
                ApiKey = t.Result.ApiKey,
                Model = t.Result.Model,
                Endpoint = t.Result.Endpoint,
                QueryEndpoint = t.Result.QueryEndpoint,
                Settings = t.Result.Settings,
            });
    }

    public async Task<AIConfig?> GetConfigById(int id)
    {
        var cfg = await _db.AiServiceConfigs.FindAsync(id);
        if (cfg == null) return null;
        return new AIConfig
        {
            Provider = cfg.Provider ?? "openai",
            BaseUrl = cfg.BaseUrl,
            ApiKey = cfg.ApiKey,
            Model = cfg.Model,
            Endpoint = cfg.Endpoint,
            QueryEndpoint = cfg.QueryEndpoint,
            Settings = cfg.Settings,
        };
    }

    public static string GetTextProviderBaseUrl(AIConfig config)
    {
        return config.Provider switch
        {
            "openai" => config.BaseUrl.TrimEnd('/'),
            "azure" => config.BaseUrl.TrimEnd('/'),
            "gemini" => config.BaseUrl.TrimEnd('/') + "/v1beta",
            _ => config.BaseUrl.TrimEnd('/')
        };
    }

    public async Task<bool> TestConnection(AIConfig config)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            var url = $"{config.BaseUrl.TrimEnd('/')}/models";
            var response = await http.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed for {Provider}", config.Provider);
            return false;
        }
    }
}
