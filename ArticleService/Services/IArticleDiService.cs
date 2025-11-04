using System.Text.Json;
using ArticleDatabase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Monitoring;
using StackExchange.Redis;

namespace ArticleService.Services;

public interface IArticleDiService
{
    Task<IEnumerable<Article>> GetArticles(string region, CancellationToken ct);
    Task<Article?> GetArticleAsync(int id, string region, CancellationToken ct = default);
    Task<List<Article>> GetRecentArticlesAsync(string region, CancellationToken ct = default);
    Task<Article> CreateArticleAsync(Article article, string region, CancellationToken ct = default);
    
     Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct = default);
     Task<Article?> UpdateArticleAsync(int id, Article updates, string region, CancellationToken ct = default);
}

public class ArticleDiService : IArticleDiService
{
    private readonly IArticleRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly CacheMetrics.ArticleCacheMetrics _metrics;

    public ArticleDiService(IArticleRepository repo, IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _repo = repo;
        _cache = cache;
        _metrics = new CacheMetrics.ArticleCacheMetrics(redis);
    }
    

    public async Task<IEnumerable<Article>> GetArticles(string region, CancellationToken ct = default)
    {
        return await _repo.GetAllArticles(region, ct);
    }


    public async Task<Article?> GetArticleAsync(int id, string region, CancellationToken ct = default)
    {
        MonitorService.Log.Information("Getting article with ID {Id}", id);
        
        var key = $"article:{region}:{id}";
        var cached = await _cache.GetStringAsync(key, ct);

        if (cached != null)
        {
            MonitorService.Log.Information("Getting article with ID {Id} from cache", id);
            await _metrics.RecordHitAsync();
            return JsonSerializer.Deserialize<Article>(cached);
        }
        MonitorService.Log.Information("Getting article with ID {Id} from repository", id);

        var article = await _repo.GetArticleById(id, region, ct);
        if (article == null) return article;
        await _metrics.RecordMissAsync();
        await _cache.SetStringAsync(
            key, JsonSerializer.Serialize(article),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
            }, ct
        );

        return article;
    }

    public async Task<List<Article>> GetRecentArticlesAsync(string region, CancellationToken ct = default)
    {
        var fortnight = DateTime.UtcNow.AddDays(-14);
        return await _repo.GetRecentArticlesAsync(region, fortnight, ct);
    }
    
    public async Task<Article> CreateArticleAsync(Article article, string region, CancellationToken ct = default)
    {
        await _repo.AddArticleAsync(article, region, ct);

        // this may be obligatory
        var key = $"article:{region}:{article.Id}";
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(article),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
            }, ct);

        return article;
    }

    public async Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct = default)
    {
        // Invalidate cache if present
        var key = $"article:{region}:{id}";
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
        {
            await _cache.RemoveAsync(key, ct);
            MonitorService.Log.Information("Invalidated cache for article {Id}", id);
        }
        
        // Delegate to repository (which already checks existence)
        var deleted = await _repo.DeleteArticleAsync(id, region, ct);
        if (deleted)
        {
            MonitorService.Log.Information("Deleted article {Id} from repository", id);
        }
        else
        {
            MonitorService.Log.Information("Article {Id} not found in repository", id);
        }
        
        return deleted;
    }

    public async Task<Article?> UpdateArticleAsync(int id, Article updates, string region, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updates);
        
        var updated = await _repo.UpdateArticleAsync(id, updates, region, ct);
        
        if (updated != null)
        {
            // Invalidate cache so next read fetches fresh data
            var key = $"article:{region}:{id}";
            await _cache.RemoveAsync(key, ct);
            MonitorService.Log.Information("Invalidated cache for updated article {Id}", id);
        }
        else
        {
            MonitorService.Log.Information("Article {Id} not found for update", id);
        }
        
        return updated;
    }
}