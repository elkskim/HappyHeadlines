using Monitoring;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
    Console.WriteLine($"MonitoringService Using Redis at {redisConnection}");
    return ConnectionMultiplexer.Connect(redisConnection);
});
//These two will absolutely not work on the first go
builder.Services.AddSingleton<CacheMetrics.ArticleCacheMetrics>();
builder.Services.AddSingleton<CacheMetrics.CommentCacheMetrics>();


var app = builder.Build();

app.MapControllers();
app.MapGet("/", () =>
{
    MonitorService.Log.Information("MonitoringService heartbeat received");
    return Results.Ok("✅ MonitoringService is running and logging");
});

app.Run();