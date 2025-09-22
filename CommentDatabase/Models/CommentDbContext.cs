using Microsoft.EntityFrameworkCore;
namespace CommentDatabase.Models;

public class CommentDbContext : DbContext
{

    public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Comment> Comments { get; set; }
}
