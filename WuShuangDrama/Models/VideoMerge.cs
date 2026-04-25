using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("video_merges")]
public class VideoMerge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("episode_id")]
    public int? EpisodeId { get; set; }

    [Column("drama_id")]
    public int? DramaId { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("provider")]
    public string? Provider { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("status")]
    public string? Status { get; set; } = "pending";

    [Column("scenes")]
    public string? Scenes { get; set; }

    [Column("merged_url")]
    public string? MergedUrl { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }

    [Column("task_id")]
    public string? TaskId { get; set; }

    [Column("error_msg")]
    public string? ErrorMsg { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("completed_at")]
    public string? CompletedAt { get; set; }

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
