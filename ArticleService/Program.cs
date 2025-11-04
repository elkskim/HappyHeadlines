using ArticleDatabase.Models;
using ArticleService.Messaging;
using ArticleService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "ArticleService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});

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
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") + ",abortConnect=false";
    return ConnectionMultiplexer.Connect(redisConnectionString);
});
builder.Services.AddHostedService<ArticleCacheCommander>();

builder.Services.AddSingleton<ArticleConsumer>();
builder.Services.AddHostedService<ArticleConsumerHostedService>();

builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379"; 
    options.InstanceName = "happyheadlines:";
});

builder.Services.AddHttpClient("CommentsService", client =>
{
    client.BaseAddress = new Uri("http://comment-service:80/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

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

Console.WriteLine("=== CALLING BUILDER.BUILD() ===");
var app = builder.Build();
Console.WriteLine("=== APP BUILT SUCCESSFULLY ===");
MonitorService.Log?.Information("Starting up {ServiceName}", serviceName);

// Log registered hosted services
Console.WriteLine("=== GETTING HOSTED SERVICES ===");
var hostedServices = app.Services.GetServices<IHostedService>();
Console.WriteLine($"=== FOUND {hostedServices.Count()} HOSTED SERVICES ===");
MonitorService.Log?.Information("Registered hosted services: {Count}", hostedServices.Count());
foreach (var service in hostedServices)
{
    Console.WriteLine($"=== HOSTED SERVICE: {service.GetType().Name} ===");
    MonitorService.Log?.Information("  - {ServiceType}", service.GetType().Name);
}

// Ensure databases exist and apply migrations with proper retry policy
Console.WriteLine("=== STARTING DATABASE INITIALIZATION ===");
await InitializeDatabasesAsync(app, regions);
Console.WriteLine("=== DATABASE INITIALIZATION COMPLETE ===");

app.UseAuthorization();
app.MapControllers();
app.Run();

// Database initialization with Polly retry policy
// Replaces the Thread.Sleep nightmare with proper async resilience patterns
static async Task InitializeDatabasesAsync(WebApplication app, Dictionary<string, string> regions)
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<DbContextFactory>();
    
    using var activity = MonitorService.ActivitySource?.StartActivity("DatabaseInitialization");
    
    var failedRegions = new List<string>();
    
    foreach (var region in regions.Keys)
    {
        try
        {
            // Polly retry policy: 5 attempts with exponential backoff (2^attempt seconds)
            // Retries on SqlException and other database-related errors
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        MonitorService.Log?.Warning(
                            "Database migration retry {RetryCount} for {Region} after {Delay}s: {Message}",
                            retryCount, region, timeSpan.TotalSeconds, exception.Message);
                    })
                .ExecuteAsync(async () =>
                {
                    // Use async context creation if available, otherwise use sync
                    await using var context = factory.CreateDbContext(new[] { "region", region });
                    
                    // Use async migration
                    await context.Database.MigrateAsync();
                    
                    MonitorService.Log?.Information("✅ {Region} database migrated successfully", region);
                });
        }
        catch (Exception ex)
        {
            // After all retries exhausted, log critical failure
            // Remember to put a cool emoji here
            MonitorService.Log?.Error(ex, 
                "❌ CRITICAL: Failed to migrate {Region} database after all retry attempts. Service may be unstable.", 
                region);
            failedRegions.Add(region);
        }
    }
    
    // If critical databases failed to migrate, log warning but allow startup
    // Regions can fail independently, and the cache still works!
    if (failedRegions.Any())
    {
        MonitorService.Log?.Warning(
            "Service starting with {Count} failed database migrations: {Regions}", 
            failedRegions.Count, 
            string.Join(", ", failedRegions));
    }
}

