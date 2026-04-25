using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("props")]
public class Prop
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("drama_id")]
    public int DramaId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    public string? Type { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("prompt")]
    public string? Prompt { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("reference_images")]
    public string? ReferenceImages { get; set; }

    [Column("local_path")]
    public string? LocalPath { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
