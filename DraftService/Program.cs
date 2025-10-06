using DraftDatabase.Data;
using DraftService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Microsoft.Extensions.Http;

//This class has been burned and born anew by intellisense and a tragic AI
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
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});


builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IDraftDiService, DraftDiService>();


builder.Services.AddHttpClient<IDraftDiService, DraftDiService>()
    .AddPolicyHandler(GetHttpRetryPolicy());


builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DraftDbContext>();

    try
    {
        context.Database.Migrate();
        MonitorService.Log.Information("{ServiceName} database migration completed", serviceName);
    }
    catch (Exception ex)
    {
        MonitorService.Log.Error(ex, "Error migrating database for {ServiceName}", serviceName);
        throw;
    }
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
