using System.Text.Json;

namespace WuShuangDrama.Adapters.Video;

public sealed class ViduVideoProvider : IVideoProvider
{
    public string ProviderName => "vidu";

    public async Task<VideoGenResponse> GenerateVideoAsync(
        AIConfig config, VideoRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "vidu-1.5",
                ["prompt"] = request.Prompt
            };

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                reqBody["images"] = new[] { request.ImageUrl };
            if (request.ReferenceImageUrls is { Count: > 0 })
                reqBody["images"] = request.ReferenceImageUrls;
            if (!string.IsNullOrWhiteSpace(request.AspectRatio))
                reqBody["aspect_ratio"] = request.AspectRatio;
            if (!string.IsNullOrWhiteSpace(request.Resolution))
                reqBody["resolution"] = request.Resolution;
            if (request.Duration.HasValue)
                reqBody["duration"] = request.Duration.Value;
            if (!string.IsNullOrWhiteSpace(request.Style))
                reqBody["style"] = request.Style;
            if (request.MotionLevel.HasValue)
                reqBody["motion_level"] = request.MotionLevel.Value;
            if (!string.IsNullOrWhiteSpace(request.CameraMotion))
                reqBody["camera_motion"] = request.CameraMotion;

            using var req = BuildRequest(config, "/v1/videos", reqBody);
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
            var url = $"{config.BaseUrl.TrimEnd('/')}/v1/videos/{taskId}";
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
            if (json.TryGetProperty("id", out var idEl) ||
                json.TryGetProperty("task_id", out idEl))
            {
                return new VideoGenResponse
                {
                    IsAsync = true,
                    TaskId = idEl.GetString()
                };
            }

            if (json.TryGetProperty("data", out var data) &&
                data.TryGetProperty("task_id", out var taskId))
            {
                return new VideoGenResponse
                {
                    IsAsync = true,
                    TaskId = taskId.GetString()
                };
            }

            return new VideoGenResponse { Error = "No task ID found in response" };
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
            var status = json.TryGetProperty("state", out var stateEl)
                ? MapViduState(stateEl.GetString())
                : json.TryGetProperty("status", out var s)
                    ? MapViduState(s.GetString())
                    : "pending";

            string? resultUrl = null;
            if (status == "done")
            {
                if (json.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("video_url", out var vu))
                {
                    resultUrl = vu.GetString();
                }
                else if (json.TryGetProperty("video_url", out var vu2))
                {
                    resultUrl = vu2.GetString();
                }
                else if (json.TryGetProperty("url", out var url))
                {
                    resultUrl = url.GetString();
                }
                else if (json.TryGetProperty("creations", out var creations) &&
                         creations.ValueKind == JsonValueKind.Array &&
                         creations.GetArrayLength() > 0)
                {
                    resultUrl = creations[0].TryGetProperty("url", out var cu)
                        ? cu.GetString()
                        : creations[0].GetString();
                }
            }

            return new PollResult { Status = status, ResultUrl = resultUrl };
        }
        catch (Exception ex)
        {
            return new PollResult { Status = "error", Error = $"Parse error: {ex.Message}" };
        }
    }

    private static string MapViduState(string? state)
    {
        return state switch
        {
            "success" or "completed" or "done" => "done",
            "failed" or "error" => "error",
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
