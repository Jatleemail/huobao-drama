using System.Text.Json;

namespace WuShuangDrama.Adapters.Video;

public sealed class AliVideoProvider : IVideoProvider
{
    public string ProviderName => "ali";

    public async Task<VideoGenResponse> GenerateVideoAsync(
        AIConfig config, VideoRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(request.Resolution))
                parameters["resolution"] = request.Resolution;
            if (!string.IsNullOrWhiteSpace(request.AspectRatio))
                parameters["aspect_ratio"] = request.AspectRatio;
            if (request.Duration.HasValue)
                parameters["duration"] = request.Duration.Value;
            if (request.Fps.HasValue)
                parameters["fps"] = request.Fps.Value;
            if (request.Seed.HasValue)
                parameters["seed"] = request.Seed.Value;

            var input = new Dictionary<string, object>
            {
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.FirstFrameUrl))
                input["first_frame_image"] = request.FirstFrameUrl;
            if (!string.IsNullOrWhiteSpace(request.LastFrameUrl))
                input["last_frame_image"] = request.LastFrameUrl;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                input["image_url"] = request.ImageUrl;
            if (request.ReferenceImageUrls is { Count: > 0 })
                input["reference_images"] = request.ReferenceImageUrls;

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "wanx-video-v1",
                ["input"] = input,
                ["parameters"] = parameters
            };

            using var req = BuildRequest(
                config, "/services/aigc/video-generation/video-synthesis", reqBody);
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
            var url = $"{config.BaseUrl.TrimEnd('/')}/services/aigc/video-generation/video-synthesis/query?task_id={taskId}";
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
            if (json.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("task_id", out var taskIdEl))
                {
                    return new VideoGenResponse
                    {
                        IsAsync = true,
                        TaskId = taskIdEl.GetString()
                    };
                }

                if (output.TryGetProperty("video_url", out var videoUrl))
                {
                    return new VideoGenResponse
                    {
                        IsAsync = false,
                        VideoUrl = videoUrl.GetString()
                    };
                }
            }

            if (json.TryGetProperty("task_id", out var taskId))
            {
                return new VideoGenResponse
                {
                    IsAsync = true,
                    TaskId = taskId.GetString()
                };
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
            var output = json.TryGetProperty("output", out var o) ? o : json;

            var status = output.TryGetProperty("task_status", out var s)
                ? MapAliStatus(s.GetString())
                : "pending";

            if (status == "done")
            {
                string? resultUrl = null;

                if (output.TryGetProperty("video_url", out var vu))
                {
                    resultUrl = vu.GetString();
                }
                else if (output.TryGetProperty("results", out var results) &&
                         results.ValueKind == JsonValueKind.Array &&
                         results.GetArrayLength() > 0)
                {
                    var first = results[0];
                    resultUrl = first.TryGetProperty("video_url", out var rvu)
                        ? rvu.GetString()
                        : first.TryGetProperty("url", out var ru)
                            ? ru.GetString()
                            : first.GetString();
                }

                return new PollResult { Status = "done", ResultUrl = resultUrl };
            }

            if (status == "error" && output.TryGetProperty("message", out var msg))
            {
                return new PollResult { Status = "error", Error = msg.GetString() };
            }

            return new PollResult { Status = status };
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
            "FAILED" or "TASK_FAILED" => "error",
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
