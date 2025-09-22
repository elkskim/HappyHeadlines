namespace CommentDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(CommentDbContext context)
    {
        var presence = context.Comments.Any();
        context.SaveChanges();
    }
}