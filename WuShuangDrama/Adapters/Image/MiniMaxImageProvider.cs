using System.Text.Json;

namespace WuShuangDrama.Adapters.Image;

public sealed class MiniMaxImageProvider : IImageProvider
{
    public string ProviderName => "minimax";

    public async Task<ImageGenResponse> GenerateImageAsync(
        AIConfig config, ImageRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "image-01",
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
                reqBody["negative_prompt"] = request.NegativePrompt;
            if (!string.IsNullOrWhiteSpace(request.Size))
                reqBody["size"] = request.Size;

            using var req = BuildRequest(config, "/image_generation", reqBody);
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

    public async Task<PollResult> PollImageAsync(
        AIConfig config, string taskId, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            var url = $"{config.BaseUrl.TrimEnd('/')}/query/image_generation?task_id={taskId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            using var res = await http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                return new PollResult { Status = "error", Error = $"HTTP {res.StatusCode}: {json}" };
            }

            using var doc = JsonDocument.Parse(json);
            return ParsePollResponse(doc.RootElement);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new PollResult { Status = "error", Error = ex.Message };
        }
    }

    public ImageGenResponse ParseGenerateResponse(JsonElement json)
    {
        try
        {
            if (json.TryGetProperty("task_id", out var taskIdEl))
            {
                return new ImageGenResponse
                {
                    IsAsync = true,
                    TaskId = taskIdEl.GetString()
                };
            }

            if (json.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("image_url", out var imgUrl))
                {
                    return new ImageGenResponse
                    {
                        IsAsync = false,
                        ImageUrl = imgUrl.GetString()
                    };
                }

                if (data.TryGetProperty("image_urls", out var imgUrls) &&
                    imgUrls.ValueKind == JsonValueKind.Array &&
                    imgUrls.GetArrayLength() > 0)
                {
                    return new ImageGenResponse
                    {
                        IsAsync = false,
                        ImageUrl = imgUrls[0].GetString()
                    };
                }
            }

            return new ImageGenResponse { Error = "Unexpected response format" };
        }
        catch (Exception ex)
        {
            return new ImageGenResponse { Error = $"Parse error: {ex.Message}" };
        }
    }

    public PollResult ParsePollResponse(JsonElement json)
    {
        try
        {
            var status = json.TryGetProperty("status", out var s)
                ? s.GetString() ?? "pending"
                : "pending";

            string? resultUrl = null;
            if (json.TryGetProperty("result", out var result))
            {
                if (result.ValueKind == JsonValueKind.String)
                {
                    resultUrl = result.GetString();
                }
                else if (result.TryGetProperty("image_url", out var imgUrl))
                {
                    resultUrl = imgUrl.GetString();
                }
            }

            return new PollResult
            {
                Status = status,
                ResultUrl = resultUrl
            };
        }
        catch (Exception ex)
        {
            return new PollResult { Status = "error", Error = $"Parse error: {ex.Message}" };
        }
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
