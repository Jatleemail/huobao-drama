using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("characters")]
public class Character
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("drama_id")]
    public int DramaId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("role")]
    public string? Role { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("appearance")]
    public string? Appearance { get; set; }

    [Column("personality")]
    public string? Personality { get; set; }

    [Column("voice_style")]
    public string? VoiceStyle { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("reference_images")]
    public string? ReferenceImages { get; set; }

    [Column("seed_value")]
    public string? SeedValue { get; set; }

    [Column("sort_order")]
    public int? SortOrder { get; set; }

    [Column("local_path")]
    public string? LocalPath { get; set; }

    [Column("voice_sample_url")]
    public string? VoiceSampleUrl { get; set; }

    [Column("voice_provider")]
    public string? VoiceProvider { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
