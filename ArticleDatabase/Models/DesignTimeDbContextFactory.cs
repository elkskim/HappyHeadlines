using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ArticleDatabase.Models;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArticleDbContext>
{
    public ArticleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ArticleDbContext>();

        // for fucks sake
        var region = "Global";

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config.GetConnectionString(region);
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"Invalid connection string: {region}");

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.EnableRetryOnFailure()
        );
        var context = new ArticleDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }
}