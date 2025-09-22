using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ArticleDatabase.Models;

public class DbContextFactory : IDesignTimeDbContextFactory<ArticleDbContext>
{
    
    public ArticleDbContext CreateDbContext(string[] args)
    {
        string region = args.Length > 0 ? args[0] : "Global";
        var optionsBuilder = new DbContextOptionsBuilder<ArticleDbContext>();
        
        
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("./ArticleService/appsettings.json")
            .Build();
        
        
        var connectionString = config.GetConnectionString(region);
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"Invalid connection string: {region}");
        
        
        ;
        optionsBuilder.UseSqlServer(connectionString);
        return new ArticleDbContext(optionsBuilder.Options);
    }

    
}