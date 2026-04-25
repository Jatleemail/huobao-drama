using System.Text.Json;

namespace WuShuangDrama.Adapters.Video;

public sealed class MiniMaxVideoProvider : IVideoProvider
{
    public string ProviderName => "minimax";

    public async Task<VideoGenResponse> GenerateVideoAsync(
        AIConfig config, VideoRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "video-01",
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.FirstFrameUrl))
                reqBody["first_frame_image"] = request.FirstFrameUrl;
            if (!string.IsNullOrWhiteSpace(request.LastFrameUrl))
                reqBody["last_frame_image"] = request.LastFrameUrl;
            if (request.ReferenceImageUrls is { Count: > 0 })
                reqBody["reference_images"] = request.ReferenceImageUrls;
            if (!string.IsNullOrWhiteSpace(request.ReferenceMode))
                reqBody["reference_mode"] = request.ReferenceMode;
            if (!string.IsNullOrWhiteSpace(request.Resolution))
                reqBody["resolution"] = request.Resolution;
            if (!string.IsNullOrWhiteSpace(request.AspectRatio))
                reqBody["aspect_ratio"] = request.AspectRatio;
            if (request.Duration.HasValue)
                reqBody["duration"] = request.Duration.Value;
            if (request.Fps.HasValue)
                reqBody["fps"] = request.Fps.Value;
            if (request.Seed.HasValue)
                reqBody["seed"] = request.Seed.Value;

            using var req = BuildRequest(config, "/video_generation", reqBody);
            using var res = await http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                return new VideoGenResponse { Error = $"HTTP {res.StatusCode}: {json}" };
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
            return new VideoGenResponse { Error = ex.Message };
        }
    }

    public async Task<PollResult> PollVideoAsync(
        AIConfig config, string taskId, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            var url = $"{config.BaseUrl.TrimEnd('/')}/query/video_generation?task_id={taskId}";
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

    public VideoGenResponse ParseGenerateResponse(JsonElement json)
    {
        try
        {
            if (json.TryGetProperty("task_id", out var taskIdEl))
            {
                return new VideoGenResponse
                {
                    IsAsync = true,
                    TaskId = taskIdEl.GetString()
                };
            }

            if (json.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("video_url", out var videoUrl))
                {
                    return new VideoGenResponse
                    {
                        IsAsync = false,
                        VideoUrl = videoUrl.GetString()
                    };
                }
            }

            return new VideoGenResponse { Error = "Unexpected response format" };
        }
        catch (Exception ex)
        {
            return new VideoGenResponse { Error = $"Parse error: {ex.Message}" };
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
            if (status == "done" || status == "Success" || status == "SUCCESS")
            {
                if (json.TryGetProperty("result", out var result))
                {
                    resultUrl = result.ValueKind == JsonValueKind.String
                        ? result.GetString()
                        : result.TryGetProperty("video_url", out var vu)
                            ? vu.GetString()
                            : null;
                }
                else if (json.TryGetProperty("data", out var data) &&
                         data.TryGetProperty("video_url", out var vu))
                {
                    resultUrl = vu.GetString();
                }
            }

            return new PollResult
            {
                Status = MapStatus(status),
                ResultUrl = resultUrl
            };
        }
        catch (Exception ex)
        {
            return new PollResult { Status = "error", Error = $"Parse error: {ex.Message}" };
        }
    }

    private static string MapStatus(string? raw)
    {
        return raw switch
        {
            "Success" or "SUCCESS" or "done" => "done",
            "Failed" or "FAILED" or "error" => "error",
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
