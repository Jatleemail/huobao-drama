using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("agent_configs")]
public class AgentConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("agent_type")]
    public string AgentType { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("temperature")]
    public double? Temperature { get; set; }

    [Column("max_tokens")]
    public int? MaxTokens { get; set; }

    [Column("max_iterations")]
    public int? MaxIterations { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
