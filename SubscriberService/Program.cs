using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monitoring;
using RabbitMQ.Client;
using Serilog;
using SubscriberDatabase.Data;
using SubscriberDatabase.Model;
using SubscriberService.Features;
using SubscriberService.Messaging;
using SubscriberService.Middleware;
using SubscriberService.Services;

namespace SubscriberService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var serviceName = "SubscriberService";
        MonitorService.Initialize(serviceName);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
        });

        var connectionString = builder.Configuration.GetConnectionString("Subscriber")
                               ?? "Server=subscriber-db,1433;Database=Subscriber;User Id=SA;Password=Pazzw0rd2025;TrustServerCertificate=True;";
        
        builder.Services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
        var featureService = builder.Services.BuildServiceProvider()
            .GetRequiredService<IFeatureToggleService>();
        var enabled = featureService.IsSubscriberServiceEnabled();
        if (enabled)
        {
            builder.Services.AddDbContext<SubscriberDbContext>(options =>
                options.UseSqlServer(connectionString));

            
            builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
            
            // Register RabbitMQ channel here to enable the DI-shenanigans
            builder.Services.AddSingleton<IChannel>(sp =>
            {
                var factory = new ConnectionFactory { HostName = "rabbitmq" };
                var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                return connection.CreateChannelAsync().GetAwaiter().GetResult();
            });
            
            builder.Services.AddSingleton<SubscriberPublisher>();
            
            builder.Services.AddScoped<ISubscriberAppService, SubscriberAppService>();
            builder.Services.AddControllers();
        }
        else
        {
            builder.Services.AddControllers();
            MonitorService.Log.Warning("SubscriberService has been disabled by feature flag.");
        }
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseMiddleware<ServiceToggleMiddleware>();
        
        if (enabled)
        {
            app.MapControllers();
        }
        else
        {
            app.MapGet("/", () => Results.Problem("SubscriberService is currently disabled.",
                statusCode: 503));
        }

        app.UseRouting();

        app.Run();
    }
}
