using System.Text.Json;

namespace WuShuangDrama.Adapters.Image;

public sealed class AliImageProvider : IImageProvider
{
    public string ProviderName => "ali";

    public async Task<ImageGenResponse> GenerateImageAsync(
        AIConfig config, ImageRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(request.Size))
                parameters["size"] = request.Size;
            if (!string.IsNullOrWhiteSpace(request.Style))
                parameters["style"] = request.Style;
            if (request.Steps.HasValue)
                parameters["steps"] = request.Steps.Value;
            if (request.CfgScale.HasValue)
                parameters["cfg_scale"] = request.CfgScale.Value;
            if (request.Seed.HasValue)
                parameters["seed"] = request.Seed.Value;
            if (request.NegativePrompt != null)
                parameters["negative_prompt"] = request.NegativePrompt;

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "wanx-v1",
                ["input"] = new Dictionary<string, object>
                {
                    ["prompt"] = request.Prompt
                },
                ["parameters"] = parameters
            };

            using var req = BuildRequest(
                config, "/services/aigc/text2image/image-synthesis", reqBody);
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
            var url = $"{config.BaseUrl.TrimEnd('/')}/services/aigc/text2image/image-synthesis/query?task_id={taskId}";
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
            if (json.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("task_id", out var taskIdEl))
                {
                    return new ImageGenResponse
                    {
                        IsAsync = true,
                        TaskId = taskIdEl.GetString()
                    };
                }

                if (output.TryGetProperty("task_status", out var statusEl) &&
                    statusEl.GetString() == "SUCCEEDED" &&
                    output.TryGetProperty("results", out var results) &&
                    results.ValueKind == JsonValueKind.Array &&
                    results.GetArrayLength() > 0)
                {
                    var first = results[0];
                    var url = first.TryGetProperty("url", out var u)
                        ? u.GetString()
                        : first.GetString();
                    return new ImageGenResponse
                    {
                        IsAsync = false,
                        ImageUrl = url
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
            if (json.TryGetProperty("output", out var output))
            {
                var status = output.TryGetProperty("task_status", out var s)
                    ? MapAliStatus(s.GetString())
                    : "pending";

                if (status == "done" &&
                    output.TryGetProperty("results", out var results) &&
                    results.ValueKind == JsonValueKind.Array &&
                    results.GetArrayLength() > 0)
                {
                    var resultUrl = results[0].TryGetProperty("url", out var u)
                        ? u.GetString()
                        : results[0].GetString();
                    return new PollResult { Status = "done", ResultUrl = resultUrl };
                }

                if (status == "error" && output.TryGetProperty("message", out var msg))
                {
                    return new PollResult { Status = "error", Error = msg.GetString() };
                }

                return new PollResult { Status = status };
            }

            return new PollResult { Status = "error", Error = "Missing output in poll response" };
        }
        catch (Exception ex)
        {
            return new PollResult { Status = "error", Error = $"Parse error: {ex.Message}" };
        }
    }

    private static string MapAliStatus(string? aliStatus)
    {
        return aliStatus switch
        {
            "SUCCEEDED" => "done",
            "FAILED" => "error",
            "TASK_FAILED" => "error",
            _ => "pending"
        };
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
