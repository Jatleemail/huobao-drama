namespace WuShuangDrama.Adapters;

public record AIConfig
{
    public string Provider { get; set; } = "openai";
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? Model { get; set; }
    public string? Endpoint { get; set; }
    public string? QueryEndpoint { get; set; }
    public string? Settings { get; set; }
}

public record ImageGenResponse
{
    public bool IsAsync { get; set; }
    public string? TaskId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Error { get; set; }
}

public record VideoGenResponse
{
    public bool IsAsync { get; set; }
    public string? TaskId { get; set; }
    public string? VideoUrl { get; set; }
    public string? Error { get; set; }
}

public record PollResult
{
    public string Status { get; set; } = "pending";
    public string? ResultUrl { get; set; }
    public string? Error { get; set; }
}

public record TTSResponse
{
    public byte[]? AudioData { get; set; }
    public string? AudioBase64 { get; set; }
    public string? Error { get; set; }
}

public record ImageRequest
{
    public string Prompt { get; set; } = "";
    public string? NegativePrompt { get; set; }
    public string? Model { get; set; }
    public string? Size { get; set; }
    public string? Quality { get; set; }
    public string? Style { get; set; }
    public int? Steps { get; set; }
    public double? CfgScale { get; set; }
    public int? Seed { get; set; }
    public List<string>? ReferenceImages { get; set; }
}

public record VideoRequest
{
    public string Prompt { get; set; } = "";
    public string? Model { get; set; }
    public string? ImageUrl { get; set; }
    public string? FirstFrameUrl { get; set; }
    public string? LastFrameUrl { get; set; }
    public List<string>? ReferenceImageUrls { get; set; }
    public string? ReferenceMode { get; set; }
    public int? Duration { get; set; }
    public int? Fps { get; set; }
    public string? Resolution { get; set; }
    public string? AspectRatio { get; set; }
    public string? Style { get; set; }
    public int? MotionLevel { get; set; }
    public string? CameraMotion { get; set; }
    public int? Seed { get; set; }
}

public record TTSRequest
{
    public string Text { get; set; } = "";
    public string? VoiceId { get; set; }
    public string? Model { get; set; }
}
