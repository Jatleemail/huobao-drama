using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("ai_service_configs")]
public class AiServiceConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("service_type")]
    public string ServiceType { get; set; } = string.Empty;

    [Column("provider")]
    public string? Provider { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [Column("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [Column("model")]
    public string? Model { get; set; }

    [Column("endpoint")]
    public string? Endpoint { get; set; }

    [Column("query_endpoint")]
    public string? QueryEndpoint { get; set; }

    [Column("priority")]
    public int? Priority { get; set; } = 0;

    [Column("is_default")]
    public bool? IsDefault { get; set; } = false;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("settings")]
    public string? Settings { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
