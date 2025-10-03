using Microsoft.EntityFrameworkCore;

namespace CommentDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(CommentDbContext context)
    {
        context.Database.Migrate();
        if (context.Comments.Any()) return;
        context.Comments.Add(new Comment("Lars", "Der problem jeg god til tromme", 1, "Global"));
        context.Comments.Add(new Comment("Pouyl", "Det løgn", 1, "Global"));
        context.Comments.Add(new Comment("Pours", "Det satme løgn", 1, "Global"));
        context.Comments.Add(new Comment("Lars", "ok så", 1, "Global"));
        
        context.SaveChanges();
    }
}