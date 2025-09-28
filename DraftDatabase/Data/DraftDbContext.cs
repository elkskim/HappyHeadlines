using DraftDatabase.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftDatabase.Data;

public class DraftDbContext : DbContext
{
    public DraftDbContext(DbContextOptions<DraftDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Draft> Drafts { get; set; }
}