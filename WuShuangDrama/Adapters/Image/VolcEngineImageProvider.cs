using System.Text.Json;

namespace WuShuangDrama.Adapters.Image;

public sealed class VolcEngineImageProvider : IImageProvider
{
    public string ProviderName => "volcengine";

    public async Task<ImageGenResponse> GenerateImageAsync(
        AIConfig config, ImageRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "doubao-seedream-4.0",
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
                reqBody["negative_prompt"] = request.NegativePrompt;
            if (!string.IsNullOrWhiteSpace(request.Size))
                reqBody["size"] = request.Size;
            if (!string.IsNullOrWhiteSpace(request.Style))
                reqBody["style"] = request.Style;
            if (request.Steps.HasValue)
                reqBody["steps"] = request.Steps.Value;
            if (request.CfgScale.HasValue)
                reqBody["cfg_scale"] = request.CfgScale.Value;
            if (request.Seed.HasValue)
                reqBody["seed"] = request.Seed.Value;

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

    public async Task<PollResult> PollImageAsync(
        AIConfig config, string taskId, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            var url = $"{config.BaseUrl.TrimEnd('/')}/query?task_id={taskId}";
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
                data.ValueKind == JsonValueKind.Array &&
                data.GetArrayLength() > 0)
            {
                var first = data[0];
                var url = first.TryGetProperty("url", out var u)
                    ? u.GetString()
                    : first.GetString();

                return new ImageGenResponse
                {
                    IsAsync = false,
                    ImageUrl = url
                };
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
            if (json.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    resultUrl = data[0].TryGetProperty("url", out var u)
                        ? u.GetString()
                        : data[0].GetString();
                }
                else if (data.TryGetProperty("url", out var urlEl))
                {
                    resultUrl = urlEl.GetString();
                }
            }

            return new PollResult { Status = status, ResultUrl = resultUrl };
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
