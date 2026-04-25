using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Data;
using WuShuangDrama.Agents.Tools;
using WuShuangDrama.Services;

namespace WuShuangDrama.Agents;

public class AgentFactory
{
    private readonly DramaDbContext _db;
    private readonly AIService _aiService;
    private readonly ILogger<AgentFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AgentFactory(DramaDbContext db, AIService aiService, ILogger<AgentFactory> logger, IServiceProvider serviceProvider)
    {
        _db = db;
        _aiService = aiService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RunAgentAsync(
        string agentType,
        int episodeId,
        int dramaId,
        string userMessage,
        CancellationToken ct = default)
    {
        var agentConfig = await _db.AgentConfigs
            .FirstOrDefaultAsync(c => c.AgentType == agentType && c.IsActive == true, ct);

        var instructions = agentType switch
        {
            "script_rewriter" => AgentInstructions.ScriptRewriter,
            "extractor" => AgentInstructions.Extractor,
            "storyboard_breaker" => AgentInstructions.StoryboardBreaker,
            "voice_assigner" => AgentInstructions.VoiceAssigner,
            "grid_prompt_generator" => AgentInstructions.GridPromptGenerator,
            _ => ""
        };

        var skillsContent = Skills.LoadAgentSkills(agentType);
        if (!string.IsNullOrEmpty(skillsContent))
            instructions += $"\n\n## Skills\n\n{skillsContent}";

        var systemPrompt = agentConfig?.SystemPrompt;
        if (string.IsNullOrEmpty(systemPrompt))
            systemPrompt = instructions;

        var config = await _aiService.GetActiveConfig("text");
        if (config == null)
            throw new InvalidOperationException("No active text AI config");

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

        var baseUrl = AIService.GetTextProviderBaseUrl(config).TrimEnd('/');
        var model = config.Model ?? "gpt-4o";

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = (double?)agentConfig?.Temperature ?? 0.7,
            max_tokens = agentConfig?.MaxTokens ?? 4096,
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync(
            $"{baseUrl}/chat/completions",
            jsonContent,
            ct);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "";
    }
}
