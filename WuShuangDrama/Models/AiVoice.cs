using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("ai_voices")]
public class AiVoice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("voice_id")]
    public string VoiceId { get; set; } = string.Empty;

    [Column("voice_name")]
    public string VoiceName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("language")]
    public string? Language { get; set; }

    [Column("provider")]
    public string Provider { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
