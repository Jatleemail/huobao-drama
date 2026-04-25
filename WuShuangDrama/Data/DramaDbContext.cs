using Microsoft.EntityFrameworkCore;
using WuShuangDrama.Models;

namespace WuShuangDrama.Data;

public class DramaDbContext : DbContext
{
    public DramaDbContext(DbContextOptions<DramaDbContext> options) : base(options) { }

    public DbSet<Drama> Dramas => Set<Drama>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<EpisodeCharacter> EpisodeCharacters => Set<EpisodeCharacter>();
    public DbSet<EpisodeScene> EpisodeScenes => Set<EpisodeScene>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Storyboard> Storyboards => Set<Storyboard>();
    public DbSet<StoryboardCharacters> StoryboardCharacters => Set<StoryboardCharacters>();
    public DbSet<AiServiceConfig> AiServiceConfigs => Set<AiServiceConfig>();
    public DbSet<AiServiceProvider> AiServiceProviders => Set<AiServiceProvider>();
    public DbSet<AiVoice> AiVoices => Set<AiVoice>();
    public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();
    public DbSet<ImageGeneration> ImageGenerations => Set<ImageGeneration>();
    public DbSet<VideoGeneration> VideoGenerations => Set<VideoGeneration>();
    public DbSet<VideoMerge> VideoMerges => Set<VideoMerge>();
    public DbSet<Prop> Props => Set<Prop>();
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoryboardCharacters>(entity =>
            entity.HasKey(e => new { e.StoryboardId, e.CharacterId }));
    }
}
