namespace WuShuangDrama.Data;

public static class DbInitializer
{
    public static void EnsureDbCreated(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DramaDbContext>();
        db.Database.EnsureCreated();
    }
}
