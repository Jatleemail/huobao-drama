using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WuShuangDrama.Data;
using WuShuangDrama.Components;
using WuShuangDrama.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "..", "data", "drama_generator.db");
builder.Services.AddDbContext<DramaDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register services
builder.Services.AddScoped<WuShuangDrama.Services.AIService>();
builder.Services.AddScoped<WuShuangDrama.Services.StorageService>();
builder.Services.AddScoped<WuShuangDrama.Services.ImageService>();
builder.Services.AddScoped<WuShuangDrama.Services.VideoService>();
builder.Services.AddScoped<WuShuangDrama.Services.TTSService>();
builder.Services.AddScoped<WuShuangDrama.Services.ComposeService>();
builder.Services.AddScoped<WuShuangDrama.Services.MergeService>();
builder.Services.AddScoped<WuShuangDrama.Services.GridService>();
builder.Services.AddScoped<WuShuangDrama.Agents.AgentFactory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "..", "data", "static")),
    RequestPath = "/static"
});
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure database exists
DbInitializer.EnsureDbCreated(app.Services);

app.Run();
