namespace CommentDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(CommentDbContext context)
    {
        context.Database.EnsureCreated();
        if (context.Comments.Any()) return;
        context.Comments.Add(new Comment("Lars", "Der problem jeg god til tromme", DateTime.Parse("10-11-1222")));
        context.Comments.Add(new Comment("Pouyl", "Det løgn", DateTime.Parse("10-11-1222")));
        context.Comments.Add(new Comment("Pours", "Det satme løgn", DateTime.Parse("10-11-1222")));
        context.Comments.Add(new Comment("Lars", "ok så", DateTime.Parse("11-11-1222")));
        context.SaveChanges();
    }
}