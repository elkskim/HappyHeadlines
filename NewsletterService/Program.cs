using Monitoring;
using NewsletterService.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "NewsletterService";

MonitorService.Initialize("NewsletterService");

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, "NewsletterService");
});


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<NewsletterConsumer>();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.Run();