using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("video_generations")]
public class VideoGeneration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("storyboard_id")]
    public int? StoryboardId { get; set; }

    [Column("drama_id")]
    public int? DramaId { get; set; }

    [Column("provider")]
    public string? Provider { get; set; }

    [Column("prompt")]
    public string? Prompt { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("image_gen_id")]
    public int? ImageGenId { get; set; }

    [Column("reference_mode")]
    public string? ReferenceMode { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("first_frame_url")]
    public string? FirstFrameUrl { get; set; }

    [Column("last_frame_url")]
    public string? LastFrameUrl { get; set; }

    [Column("reference_image_urls")]
    public string? ReferenceImageUrls { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }

    [Column("fps")]
    public int? Fps { get; set; }

    [Column("resolution")]
    public string? Resolution { get; set; }

    [Column("aspect_ratio")]
    public string? AspectRatio { get; set; }

    [Column("style")]
    public string? Style { get; set; }

    [Column("motion_level")]
    public int? MotionLevel { get; set; }

    [Column("camera_motion")]
    public string? CameraMotion { get; set; }

    [Column("seed")]
    public int? Seed { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("minio_url")]
    public string? MinioUrl { get; set; }

    [Column("local_path")]
    public string? LocalPath { get; set; }

    [Column("status")]
    public string? Status { get; set; } = "pending";

    [Column("task_id")]
    public string? TaskId { get; set; }

    [Column("error_msg")]
    public string? ErrorMsg { get; set; }

    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("completed_at")]
    public string? CompletedAt { get; set; }

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
