// See https://aka.ms/new-console-template for more information
using ArticleDatabase;
using ArticleDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add DbContext (points to Docker DB connection string)
/*
builder.Services.AddDbContext<ArticleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    */
builder.Services.AddSingleton<DbContextFactory>();

// Add controllers
builder.Services.AddControllers();

// Swagger for dev
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
