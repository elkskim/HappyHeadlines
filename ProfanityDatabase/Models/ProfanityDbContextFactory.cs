using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProfanityDatabase.Models;

public class ProfanityDbContextFactory : IDesignTimeDbContextFactory<ProfanityDbContext>
{
    public ProfanityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProfanityDbContext>();


        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
        
        
        var connectionString = config.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Invalid connection string");

        optionsBuilder.UseSqlServer((string?)connectionString);
        
        return new ProfanityDbContext(optionsBuilder.Options);
    }
    
    
}