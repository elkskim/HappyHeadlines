using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using ProfanityDatabase.Models;
using ProfanityService.Services;
using Monitoring;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<ProfanityDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Profanity"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        ));
});
builder.Services.AddTransient<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<IProfanityDiService, ProfanityDiService>();

builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var serviceName = "ProfanityService";

MonitorService.Initialize(serviceName);

builder.Host.UseSerilog((context, services, configuration) =>
{
    MonitorService.ConfigureSerilog(context, services, configuration, serviceName);
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    var context = scope.ServiceProvider.GetRequiredService<ProfanityDbContext>();
    initializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseAuthorization();

app.MapControllers();

app.Run();

