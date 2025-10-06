using DraftDatabase.Data;
using DraftService.Services;
using Microsoft.EntityFrameworkCore;
using Monitoring;
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


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DraftDbContext>();

        context.Database.Migrate();
        MonitorService.Log.Information("{ServiceName} database migration completed", serviceName);
  
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
