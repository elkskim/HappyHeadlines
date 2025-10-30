using Monitoring;
using NewsletterService.Features;
using NewsletterService.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "NewsletterService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});

builder.Services.AddSingleton<IFeatureToggleService, FeatureToggleService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// We now have two consumers. The Copilot refuses to let me write swear words in comments.
builder.Services.AddSingleton<NewsletterArticleConsumer>();
builder.Services.AddHostedService<NewsletterArticleConsumerHostedService>();

builder.Services.AddSingleton<NewsletterSubscriberConsumer>();
builder.Services.AddHostedService<NewsletterSubscriberConsumerHostedService>();

builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

MonitorService.Log.Information("Starting up {ServiceName}", serviceName);
app.MapControllers();

app.Run();