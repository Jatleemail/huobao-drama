using System.Text.Json;

namespace WuShuangDrama.Adapters.Image;

public sealed class OpenAIImageProvider : IImageProvider
{
    public string ProviderName => "openai";

    public async Task<ImageGenResponse> GenerateImageAsync(
        AIConfig config, ImageRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["prompt"] = request.Prompt,
                ["model"] = request.Model ?? config.Model ?? "dall-e-3",
                ["n"] = 1,
                ["size"] = request.Size ?? "1024x1024",
                ["response_format"] = "url"
            };

            if (!string.IsNullOrWhiteSpace(request.Quality))
                reqBody["quality"] = request.Quality;
            if (!string.IsNullOrWhiteSpace(request.Style))
                reqBody["style"] = request.Style;

            using var req = BuildRequest(config, "/images/generations", reqBody);
            using var res = await http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ImageGenResponse { Error = $"HTTP {res.StatusCode}: {json}" };
            }

            using var doc = JsonDocument.Parse(json);
            return ParseGenerateResponse(doc.RootElement);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ImageGenResponse { Error = ex.Message };
        }
    }

    public Task<PollResult> PollImageAsync(
        AIConfig config, string taskId, CancellationToken ct = default)
    {
        return Task.FromResult(new PollResult { Status = "done" });
    }

    public ImageGenResponse ParseGenerateResponse(JsonElement json)
    {
        try
        {
            var data = json.GetProperty("data");
            var first = data[0];
            var url = first.GetProperty("url").GetString();

            return new ImageGenResponse
            {
                IsAsync = false,
                ImageUrl = url
            };
        }
        catch (Exception ex)
        {
            return new ImageGenResponse { Error = $"Parse error: {ex.Message}" };
        }
    }

    public PollResult ParsePollResponse(JsonElement json)
    {
        return new PollResult { Status = "done" };
    }

    private static HttpRequestMessage BuildRequest(
        AIConfig config, string path, Dictionary<string, object> body)
    {
        var url = (config.BaseUrl ?? "").TrimEnd('/') + path;
        var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
        msg.Content = JsonContent.Create(body);
        return msg;
    }
}
