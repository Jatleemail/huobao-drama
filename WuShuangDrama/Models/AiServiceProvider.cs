using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("ai_service_providers")]
public class AiServiceProvider
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("service_type")]
    public string ServiceType { get; set; } = string.Empty;

    [Column("provider")]
    public string Provider { get; set; } = string.Empty;

    [Column("default_url")]
    public string? DefaultUrl { get; set; }

    [Column("preset_models")]
    public string? PresetModels { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
