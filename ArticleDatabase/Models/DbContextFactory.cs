using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArticleDatabase.Models;

public class DbContextFactory
{
    private readonly IConfiguration _configuration;

    public DbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ArticleDbContext CreateDbContext(string region)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ArticleDbContext>();
        
        var connectionString = _configuration.GetConnectionString(region);
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"Invalid connection string: {region}");
        ;
        optionsBuilder.UseSqlServer(connectionString);
        return new ArticleDbContext(optionsBuilder.Options);
    }
}