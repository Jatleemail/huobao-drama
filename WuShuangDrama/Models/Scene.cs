using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("scenes")]
public class Scene
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("drama_id")]
    public int DramaId { get; set; }

    [Column("episode_id")]
    public int? EpisodeId { get; set; }

    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("time")]
    public string Time { get; set; } = string.Empty;

    [Column("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [Column("storyboard_count")]
    public int? StoryboardCount { get; set; } = 1;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("status")]
    public string? Status { get; set; } = "pending";

    [Column("local_path")]
    public string? LocalPath { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
