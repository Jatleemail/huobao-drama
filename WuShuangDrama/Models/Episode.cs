using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("episodes")]
public class Episode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("drama_id")]
    public int DramaId { get; set; }

    [Column("episode_number")]
    public int EpisodeNumber { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("script_content")]
    public string? ScriptContent { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("duration")]
    public int? Duration { get; set; } = 0;

    [Column("status")]
    public string? Status { get; set; } = "draft";

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("thumbnail")]
    public string? Thumbnail { get; set; }

    [Column("image_config_id")]
    public int? ImageConfigId { get; set; }

    [Column("video_config_id")]
    public int? VideoConfigId { get; set; }

    [Column("audio_config_id")]
    public int? AudioConfigId { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
