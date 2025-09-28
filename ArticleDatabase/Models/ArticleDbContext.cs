using Microsoft.EntityFrameworkCore;

namespace ArticleDatabase.Models;

public class ArticleDbContext : DbContext
{
    public ArticleDbContext(DbContextOptions<ArticleDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles { get; set; }
}