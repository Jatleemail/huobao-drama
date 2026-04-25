using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuShuangDrama.Models;

[Table("storyboards")]
public class Storyboard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("episode_id")]
    public int EpisodeId { get; set; }

    [Column("scene_id")]
    public int? SceneId { get; set; }

    [Column("storyboard_number")]
    public int StoryboardNumber { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Column("time")]
    public string? Time { get; set; }

    [Column("shot_type")]
    public string? ShotType { get; set; }

    [Column("angle")]
    public string? Angle { get; set; }

    [Column("movement")]
    public string? Movement { get; set; }

    [Column("action")]
    public string? Action { get; set; }

    [Column("result")]
    public string? Result { get; set; }

    [Column("atmosphere")]
    public string? Atmosphere { get; set; }

    [Column("image_prompt")]
    public string? ImagePrompt { get; set; }

    [Column("video_prompt")]
    public string? VideoPrompt { get; set; }

    [Column("bgm_prompt")]
    public string? BgmPrompt { get; set; }

    [Column("sound_effect")]
    public string? SoundEffect { get; set; }

    [Column("dialogue")]
    public string? Dialogue { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("duration")]
    public int? Duration { get; set; } = 0;

    [Column("composed_image")]
    public string? ComposedImage { get; set; }

    [Column("first_frame_image")]
    public string? FirstFrameImage { get; set; }

    [Column("last_frame_image")]
    public string? LastFrameImage { get; set; }

    [Column("reference_images")]
    public string? ReferenceImages { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("tts_audio_url")]
    public string? TtsAudioUrl { get; set; }

    [Column("subtitle_url")]
    public string? SubtitleUrl { get; set; }

    [Column("composed_video_url")]
    public string? ComposedVideoUrl { get; set; }

    [Column("status")]
    public string? Status { get; set; } = "pending";

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }
}
