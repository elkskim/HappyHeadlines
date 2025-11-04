using Monitoring;
using PublisherService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "PublisherService";

MonitorService.Initialize("PublisherService");

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, "PublisherService");
});


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PublisherMessaging>();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();