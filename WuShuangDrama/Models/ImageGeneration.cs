using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("image_generations")]
public class ImageGeneration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("storyboard_id")]
    public int? StoryboardId { get; set; }

    [Column("drama_id")]
    public int? DramaId { get; set; }

    [Column("scene_id")]
    public int? SceneId { get; set; }

    [Column("character_id")]
    public int? CharacterId { get; set; }

    [Column("prop_id")]
    public int? PropId { get; set; }

    [Column("image_type")]
    public string? ImageType { get; set; }

    [Column("frame_type")]
    public string? FrameType { get; set; }

    [Column("provider")]
    public string? Provider { get; set; }

    [Column("prompt")]
    public string? Prompt { get; set; }

    [Column("negative_prompt")]
    public string? NegativePrompt { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("size")]
    public string? Size { get; set; }

    [Column("quality")]
    public string? Quality { get; set; }

    [Column("style")]
    public string? Style { get; set; }

    [Column("steps")]
    public int? Steps { get; set; }

    [Column("cfg_scale")]
    public double? CfgScale { get; set; }

    [Column("seed")]
    public int? Seed { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

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

    [Column("reference_images")]
    public string? ReferenceImages { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("completed_at")]
    public string? CompletedAt { get; set; }
}
