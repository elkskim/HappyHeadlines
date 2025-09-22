namespace CommentDatabase.Models;

public interface IDbInitializer
{
    void Initialize(CommentDbContext context);
}