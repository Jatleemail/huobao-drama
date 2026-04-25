using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("dramas")]
public class Drama
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("genre")]
    public string? Genre { get; set; }

    [Column("style")]
    public string? Style { get; set; } = "realistic";

    [Column("total_episodes")]
    public int? TotalEpisodes { get; set; } = 1;

    [Column("total_duration")]
    public int? TotalDuration { get; set; } = 0;

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("thumbnail")]
    public string? Thumbnail { get; set; }

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
