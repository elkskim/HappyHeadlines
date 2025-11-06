﻿using Microsoft.AspNetCore.Builder;
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

public partial class Program
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
            // Now with retry logic, so the service does not crash when RabbitMQ is not ready
            builder.Services.AddSingleton<IChannel>(sp =>
            {
                var factory = new ConnectionFactory { HostName = "rabbitmq" };
                
                int attempt = 0;
                int maxAttempts = 10;
                int delayMs = 2000;
                
                while (attempt < maxAttempts)
                {
                    try
                    {
                        MonitorService.Log.Information("Connecting to RabbitMQ (attempt {Attempt}/{Max})", attempt + 1, maxAttempts);
                        var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
                        MonitorService.Log.Information("Successfully connected to RabbitMQ");
                        return channel;
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        if (attempt >= maxAttempts)
                        {
                            MonitorService.Log.Error(ex, "Failed to connect to RabbitMQ after {Attempts} attempts", maxAttempts);
                            throw;
                        }
                        
                        MonitorService.Log.Warning(ex, "RabbitMQ connection failed (attempt {Attempt}/{Max}); retrying in {Delay}ms", attempt, maxAttempts, delayMs);
                        Thread.Sleep(delayMs);
                        delayMs = Math.Min(delayMs * 2, 30000);
                    }
                }
                
                throw new InvalidOperationException("Failed to connect to RabbitMQ; retry loop exhausted without success");
            });
            
            // Register publisher as its interface so tests can mock ISubscriberPublisher
            builder.Services.AddSingleton<ISubscriberPublisher>(sp =>
            {
                var channel = sp.GetRequiredService<IChannel>();
                return new SubscriberPublisher(channel);
            });
            
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

        // Initialize database (create and migrate if needed)
        if (enabled)
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubscriberDbContext>();
                try
                {
                    MonitorService.Log.Information("Ensuring SubscriberDatabase is created and migrated...");
                    db.Database.Migrate();
                    MonitorService.Log.Information("SubscriberDatabase ready");
                }
                catch (Exception ex)
                {
                    MonitorService.Log.Error(ex, "Failed to migrate SubscriberDatabase");
                    throw;
                }
            }
        }

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
