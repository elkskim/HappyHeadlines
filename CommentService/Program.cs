using CommentDatabase.Models;
using CommentService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using StackExchange.Redis;

//This class has been burned and born anew by intellisense and a tragic AI
var builder = WebApplication.CreateBuilder(args);

const string serviceName = "CommentService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<CommentDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Comment"),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

builder.Services.AddTransient<IDbInitializer, DbInitializer>();


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
});

//sure hope this one is necessary
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConfig = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
    return ConnectionMultiplexer.Connect(redisConfig);
});



builder.Services.AddScoped<ICommentCacheCommander, CommentCacheCommander>();
builder.Services.AddScoped<IResilienceService, ResilienceService>();


builder.Services.AddHttpClient("ProfanityService", client =>
    {
        client.BaseAddress = new Uri("http://profanity-service:80/");
    })
    .AddPolicyHandler(GetHttpRetryPolicy());


builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<IDbInitializer>();
    var context = services.GetRequiredService<CommentDbContext>();

    try
    {
        initializer.Initialize(context);
    }
    catch (Exception ex)
    {
        MonitorService.Log.Error(ex, "Failed to initialize database for {ServiceName}", serviceName);
        throw;
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

MonitorService.Log.Information("Starting up {ServiceName}", serviceName);

app.UseAuthorization();
app.MapControllers();
app.Run();
return;


static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"HTTP retry {retryAttempt} after {timespan.TotalSeconds}s");
            });
}
