using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("storyboard_characters")]
public class StoryboardCharacters
{
    [Column("storyboard_id")]
    public int StoryboardId { get; set; }

    [Column("character_id")]
    public int CharacterId { get; set; }
}
