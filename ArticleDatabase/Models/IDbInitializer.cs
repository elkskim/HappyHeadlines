namespace ArticleDatabase.Models;

public interface IDbInitializer
{
    void Initialize(ArticleDbContext context);
}