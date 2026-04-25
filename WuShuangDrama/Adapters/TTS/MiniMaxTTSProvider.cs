using System.Text.Json;

namespace WuShuangDrama.Adapters.TTS;

public sealed class MiniMaxTTSProvider : ITTSProvider
{
    public string ProviderName => "minimax";

    public async Task<TTSResponse> GenerateTTSAsync(
        AIConfig config, TTSRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();

            var reqBody = new Dictionary<string, object>
            {
                ["model"] = request.Model ?? config.Model ?? "speech-01-turbo",
                ["text"] = request.Text,
                ["stream"] = false
            };

            if (!string.IsNullOrWhiteSpace(request.VoiceId))
                reqBody["voice_id"] = request.VoiceId;

            using var req = BuildRequest(config, "/t2a_v2", reqBody);
            using var res = await http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                return new TTSResponse { Error = $"HTTP {res.StatusCode}: {json}" };
            }

            using var doc = JsonDocument.Parse(json);
            return ParseResponse(doc.RootElement);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TTSResponse { Error = ex.Message };
        }
    }

    private static TTSResponse ParseResponse(JsonElement json)
    {
        try
        {
            if (json.TryGetProperty("data", out var data) &&
                data.TryGetProperty("audio", out var audioEl))
            {
                var hexString = audioEl.GetString();
                if (!string.IsNullOrWhiteSpace(hexString))
                {
                    var audioBytes = HexToBytes(hexString);
                    return new TTSResponse { AudioData = audioBytes };
                }
            }

            if (json.TryGetProperty("audio", out var audioDirect))
            {
                var base64 = audioDirect.GetString();
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    return new TTSResponse { AudioBase64 = base64 };
                }
            }

            return new TTSResponse { Error = "No audio data in response" };
        }
        catch (Exception ex)
        {
            return new TTSResponse { Error = $"Parse error: {ex.Message}" };
        }
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length");

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
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
