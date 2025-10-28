using Microsoft.EntityFrameworkCore;
using SubscriberDatabase.Model;

namespace SubscriberDatabase.Data;

public class SubscriberDbContext : DbContext
{
    public SubscriberDbContext(DbContextOptions<SubscriberDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Subscriber> Subscribers { get; set; }
}