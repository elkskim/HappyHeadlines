using Microsoft.EntityFrameworkCore;

namespace ArticleDatabase.Models;

public interface IArticleRepository
{
    Task<Article?> GetArticleById(int id, string region, CancellationToken cancellationToken);
    Task<List<Article>> GetAllArticles( string region,CancellationToken cancellationToken);
    Task<List<Article>> GetRecentArticlesAsync(string region, DateTime since, CancellationToken cancellationToken);
    Task AddArticleAsync(Article article, string region, CancellationToken cancellationToken);
    Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct);
    Task<Article?> UpdateArticleAsync(int id, Article updates, string region, CancellationToken ct);
}

//Gotta go fast, here's everything in one disgusting file

public class ArticleRepository : IArticleRepository
{
    private readonly DbContextFactory _db;
    
    public ArticleRepository(DbContextFactory db)
    {
        _db = db;
    }

    public async Task<Article?> GetArticleById(int id, string region, CancellationToken cancellationToken = default)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        return await db.Articles.FindAsync([id], cancellationToken);
    }

    public async Task<List<Article>> GetAllArticles( string region, CancellationToken cancellationToken)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        return await db.Articles.ToListAsync(cancellationToken);
    }

    public async Task<List<Article>> GetRecentArticlesAsync( string region, DateTime since, CancellationToken cancellationToken = default)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        return await db.Articles
            .Where(a => a.Created >= since)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddArticleAsync(Article article, string region, CancellationToken cancellationToken = default)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        await db.Articles.AddAsync(article, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        var article = await db.Articles.FindAsync([id], ct);
        if (article == null)
        {
            return false;
        }
        db.Articles.Remove(article);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Article?> UpdateArticleAsync(int id, Article updates, string region, CancellationToken ct)
    {
        await using var db = _db.CreateDbContext(["region", region]);
        var article = await db.Articles.FindAsync([id], ct);
        if (article == null)
        {
            return null;
        }
        article.Title = updates.Title;
        article.Content = updates.Content;
        article.Author = updates.Author;
        await db.SaveChangesAsync(ct);
        return article;
    }
}