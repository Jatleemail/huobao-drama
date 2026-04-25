using System.Text.Json;

namespace WuShuangDrama.Adapters.Image;

public sealed class GeminiImageProvider : IImageProvider
{
    public string ProviderName => "gemini";

    public async Task<ImageGenResponse> GenerateImageAsync(
        AIConfig config, ImageRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var model = request.Model ?? config.Model ?? "gemini-2.0-flash-exp";
            var url = $"{config.BaseUrl.TrimEnd('/')}/{model}:generateContent";

            var parts = new List<object>
            {
                new { text = request.Prompt }
            };

            var reqBody = new
            {
                contents = new[]
                {
                    new { parts = parts.ToArray() }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "IMAGE" }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", config.ApiKey);
            req.Content = JsonContent.Create(reqBody);
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
            var base64 = ExtractImageBase64(json);
            if (!string.IsNullOrWhiteSpace(base64))
            {
                return new ImageGenResponse
                {
                    IsAsync = false,
                    ImageUrl = base64
                };
            }

            if (json.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var content = candidates[0].GetProperty("content");
                if (content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("fileData", out var fileData))
                        {
                            var fileUrl = fileData.GetProperty("fileUri").GetString();
                            return new ImageGenResponse
                            {
                                IsAsync = false,
                                ImageUrl = fileUrl
                            };
                        }
                    }
                }
            }

            return new ImageGenResponse { Error = "No image data found in response" };
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

    public static string? ExtractImageBase64(JsonElement json)
    {
        try
        {
            if (json.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var content = candidates[0].GetProperty("content");
                if (content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("inlineData", out var inlineData))
                        {
                            var mimeType = inlineData.GetProperty("mimeType").GetString();
                            var data = inlineData.GetProperty("data").GetString();
                            if (!string.IsNullOrWhiteSpace(data))
                            {
                                return mimeType?.StartsWith("image/") == true
                                    ? $"data:{mimeType};base64,{data}"
                                    : data;
                            }
                        }
                    }
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
