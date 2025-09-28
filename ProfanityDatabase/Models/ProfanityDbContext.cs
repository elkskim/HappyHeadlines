using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions;
using Microsoft.Extensions.Configuration;

namespace ProfanityDatabase.Models;

public class ProfanityDbContext : DbContext
{
    public ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Profanity> Profanities { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        /*
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("./ProfanityService/appsettings.json")
            .Build();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("Profanity"));
        */
    }
}