using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("assets")]
public class Asset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("drama_id")]
    public int? DramaId { get; set; }

    [Column("episode_id")]
    public int? EpisodeId { get; set; }

    [Column("storyboard_id")]
    public int? StoryboardId { get; set; }

    [Column("storyboard_num")]
    public int? StoryboardNum { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("local_path")]
    public string? LocalPath { get; set; }

    [Column("file_size")]
    public int? FileSize { get; set; }

    [Column("mime_type")]
    public string? MimeType { get; set; }

    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }

    [Column("format")]
    public string? Format { get; set; }

    [Column("image_gen_id")]
    public int? ImageGenId { get; set; }

    [Column("video_gen_id")]
    public int? VideoGenId { get; set; }

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("view_count")]
    public int? ViewCount { get; set; } = 0;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
