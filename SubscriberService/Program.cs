using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monitoring;
using Serilog;
using SubscriberDatabase.Data;
using SubscriberDatabase.Model;
using SubscriberService.Services;

namespace SubscriberService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // using chatgpt for some beautiful formatting. it even added emojis
        // so that i can fit in with LinkedIn people
        
        // -------------------------------
        // 1️⃣ Monitoring Initialization
        // -------------------------------
        var serviceName = "SubscriberService";
        MonitorService.Initialize(serviceName);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
        });

        // -------------------------------
        // 2️⃣ Configuration & Connection String
        // -------------------------------
        var connectionString = builder.Configuration.GetConnectionString("Subscriber")
                               ?? "Server=subscriber-db,1433;Database=Subscriber;User Id=SA;Password=Pazzw0rd2025;TrustServerCertificate=True;";

        // -------------------------------
        // 3️⃣ Register DbContext
        // -------------------------------
        builder.Services.AddDbContext<SubscriberDbContext>(options =>
            options.UseSqlServer(connectionString));

        // -------------------------------
        // 4️⃣ Register Repository + AppService
        // -------------------------------
        builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
        builder.Services.AddScoped<ISubscriberAppService, SubscriberAppService>();

        // -------------------------------
        // 5️⃣ Add Controllers, Swagger, etc.
        // -------------------------------
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // -------------------------------
        // 6️⃣ Build and Run
        // -------------------------------
        var app = builder.Build();

        
        // time to actually utilize swaggergen
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.MapControllers();

        app.Run();
    }
}
