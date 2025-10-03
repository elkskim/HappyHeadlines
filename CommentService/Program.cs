using CommentDatabase.Models;
using CommentService.Services;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Comment")));
builder.Services.AddTransient<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<IResilienceService, ResilienceService>();

builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(80));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    var context = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    initializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // shows full stack traces
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Profinity API v1");
        c.RoutePrefix = "swagger"; // ensures /swagger works
    });
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();