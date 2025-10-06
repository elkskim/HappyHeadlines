using Monitoring;
using NewsletterService.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "NewsletterService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<NewsletterConsumer>();
builder.Services.AddHostedService<NewsletterConsumerHostedService>();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

MonitorService.Log.Information("Starting up {ServiceName}", serviceName);
app.MapControllers();

app.Run();