using System.Text.Json;

namespace WuShuangDrama.Adapters.Video;

public sealed class VolcEngineVideoProvider : IVideoProvider
{
    public string ProviderName => "volcengine";

    public async Task<VideoGenResponse> GenerateVideoAsync(
        AIConfig config, VideoRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "doubao-seedance-1.0-pro",
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                reqBody["image_url"] = request.ImageUrl;
            if (!string.IsNullOrWhiteSpace(request.FirstFrameUrl))
                reqBody["first_frame_url"] = request.FirstFrameUrl;
            if (!string.IsNullOrWhiteSpace(request.LastFrameUrl))
                reqBody["last_frame_url"] = request.LastFrameUrl;
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

            if (request.ReferenceImageUrls is { Count: > 0 })
                reqBody["reference_images"] = request.ReferenceImageUrls;
            if (!string.IsNullOrWhiteSpace(request.ReferenceMode))
                reqBody["reference_mode"] = request.ReferenceMode;

            using var req = BuildRequest(config, "/videos/generations", reqBody);
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
                if (data.ValueKind == JsonValueKind.Array &&
                    data.GetArrayLength() > 0)
                {
                    var url = data[0].TryGetProperty("url", out var u)
                        ? u.GetString()
                        : data[0].GetString();
                    return new VideoGenResponse
                    {
                        IsAsync = false,
                        VideoUrl = url
                    };
                }

                if (data.TryGetProperty("video_url", out var vu))
                {
                    return new VideoGenResponse
                    {
                        IsAsync = false,
                        VideoUrl = vu.GetString()
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
            if (status == "done" || status == "completed")
            {
                if (json.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Array &&
                        data.GetArrayLength() > 0)
                    {
                        resultUrl = data[0].TryGetProperty("url", out var u)
                            ? u.GetString()
                            : data[0].GetString();
                    }
                    else if (data.TryGetProperty("video_url", out var vu))
                    {
                        resultUrl = vu.GetString();
                    }
                    else if (data.TryGetProperty("url", out var urlEl))
                    {
                        resultUrl = urlEl.GetString();
                    }
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
