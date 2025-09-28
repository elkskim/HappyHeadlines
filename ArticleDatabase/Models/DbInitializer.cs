namespace ArticleDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(ArticleDbContext context)
    {
            context.Database.EnsureCreated();
            if (context.Articles.Any()) return;
            context.Articles.Add(new Article("NewArticle1", "This is an article hahaha numba 1", "Donald J Trump"));
            context.Articles.Add(new Article("Another one", "This is DJ Khaled, bring em the ocean", "DJ Khaled "));
            context.Articles.Add(new Article("Last test", "jesus christ it's jason bourne", "some guy"));
            context.SaveChanges();
        
    }
}