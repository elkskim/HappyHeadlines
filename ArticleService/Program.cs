using ArticleDatabase.Models;
using ArticleService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", false, true);

// Register services
builder.Services.AddDbContext<ArticleDbContext>();
builder.Services.AddSingleton<DbContextFactory>();
builder.Services.AddScoped<DesignTimeDbContextFactory>();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IArticleDiService, ArticleDiService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
builder.Services.AddHostedService<ArticleCacheCommander>();

builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379"; // docker compose alias
    options.InstanceName = "happyheadlines:";
});

// Define regions and their container hostnames/ports
var regions = new Dictionary<string, string>
{
    ["Global"] = "global-article-db,1433",
    ["Africa"] = "africa-article-db,1433",
    ["Asia"] = "asia-article-db,1433",
    ["Europe"] = "europe-article-db,1433",
    ["NorthAmerica"] = "northamerica-article-db,1433",
    ["SouthAmerica"] = "southamerica-article-db,1433",
    ["Oceania"] = "oceania-article-db,1433",
    ["Antarctica"] = "antarctica-article-db,1433"
};

var app = builder.Build();

// Ensure databases exist & apply migrations
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<DbContextFactory>();

    using var activity = MonitorService.ActivitySource.StartActivity();

    foreach (var region in regions.Keys)
        try
        {
            Thread.Sleep(1000);
            using var context = factory.CreateDbContext(["region", region]);
            context.Database.Migrate(); // creates DB if missing, applies migrations
            MonitorService.Log.Debug("✅ {Region} database ensured/migrated.", region);
        }
        catch (Exception ex)
        {
            Thread.Sleep(1000);
            MonitorService.Log.Debug("❌ Failed to migrate {Region}: {ExMessage}", region, ex.Message);
        }
}

app.UseAuthorization();
app.MapControllers();
app.Run();