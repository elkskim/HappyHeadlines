using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArticleDatabase.Models;

public class DbContextFactory
{
    public ArticleDbContext CreateDbContext(string[] args)
    {
        var region = args.Length > 1 ? args[1] : "Global";
        var optionsBuilder = new DbContextOptionsBuilder<ArticleDbContext>();


        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        var connectionName = runningInContainer ? region : region + "Host";
        var connectionString = config.GetConnectionString(connectionName);

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"Invalid connection string: {region}");


        ; //, sqlOptions => sqlOptions.EnableRetryOnFailure()
        optionsBuilder.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
        var context = new ArticleDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        context.Database.Migrate();
        return context;
        ;
    }
}