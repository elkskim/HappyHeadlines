using Microsoft.EntityFrameworkCore;
using ProfanityDatabase.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddTransient<IDbInitializer, DbInitializer>();
builder.Services.AddDbContext<ProfanityDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("Profanity")));
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
