using DraftDatabase.Data;
using DraftService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "DraftService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<DraftDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Draft"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                        
                maxRetryDelay: TimeSpan.FromSeconds(10), 
                errorNumbersToAdd: null                   
            );
        });
});


builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IDraftDiService, DraftDiService>();


builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var app = builder.Build();

// Apply database migrations with retry logic to handle SQL Server startup delays
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DraftDbContext>();
    
    // Retry policy: 5 attempts with 10-second delays between attempts
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            5, 
            _ => TimeSpan.FromSeconds(10),
            (exception, timeSpan, retryCount, _) =>
            {
                MonitorService.Log.Warning(
                    "{ServiceName} migration attempt {RetryCount} failed: {Message}. Retrying in {Seconds}s...",
                    serviceName, retryCount, exception.Message, timeSpan.TotalSeconds);
            });
    
    await retryPolicy.ExecuteAsync(async () =>
    {
        await context.Database.MigrateAsync();
        MonitorService.Log.Information("{ServiceName} database migration completed", serviceName);
    });
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

MonitorService.Log.Information("Starting up {ServiceName}", serviceName);

app.UseAuthorization();
app.MapControllers();
app.Run();
