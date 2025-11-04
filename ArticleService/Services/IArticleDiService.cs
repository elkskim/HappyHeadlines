using System.Text.Json;
using ArticleDatabase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _memoryCache;

    public ArticleDiService(IArticleRepository repo, IDistributedCache cache, IConnectionMultiplexer redis, IMemoryCache memoryCache)
    {
        _repo = repo;
        _cache = cache;
        _metrics = new CacheMetrics.ArticleCacheMetrics(redis);
        _memoryCache = memoryCache;
    }
    

    public async Task<IEnumerable<Article>> GetArticles(string region, CancellationToken ct = default)
    {
        return await _repo.GetAllArticles(region, ct);
    }


    public async Task<Article?> GetArticleAsync(int id, string region, CancellationToken ct = default)
    {
        MonitorService.Log.Information("Getting article with ID {Id}", id);

        var key = $"article:{region}:{id}";

        // Trying to make one of em local caches
        if (_memoryCache.TryGetValue(key, out Article? cachedArticle))
        {
            MonitorService.Log.Information("Article with ID {Id} found in local memory cache", id);
            await _metrics.RecordHitAsync();
            return cachedArticle;
        }

        // Check Redis cache
        var redisCached = await _cache.GetStringAsync(key, ct);
        if (redisCached != null)
        {
            var article = JsonSerializer.Deserialize<Article>(redisCached);
            if (article != null)
            {
                // Stick this fella in local cache too
                _memoryCache.Set(key, article, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                MonitorService.Log.Information("Article with ID {Id} found in Redis cache", id);
                await _metrics.RecordHitAsync();
                
                return article;
            }
        }
        
        // Cache miss; fetch from repository
        MonitorService.Log.Information("Cache miss for article with ID {Id}; fetching from repository", id);
        await _metrics.RecordMissAsync();
        
        var fetchedArticle =  await _repo.GetArticleById(id, region, ct);
        if (fetchedArticle != null)
        {
            // Warm up those caches, from where they were missed dearly
            var json = JsonSerializer.Serialize(fetchedArticle);
            await _cache.SetStringAsync(key, json, ct);
            _memoryCache.Set(key, fetchedArticle, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        }
        
        return fetchedArticle;
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
        // Invalidate both cache layers (memory + Redis)
        var key = $"article:{region}:{id}";
        _memoryCache.Remove(key);
        await _cache.RemoveAsync(key, ct);
        MonitorService.Log.Information("Invalidated Inmem/Redis cache for article {Id}", id);
        
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
            // Invalidate both cache layers (memory + Redis)
            var key = $"article:{region}:{id}";
            _memoryCache.Remove(key);
            await _cache.RemoveAsync(key, ct);
            MonitorService.Log.Information("Invalidated Redis/inMemory cache for updated article {Id}", id);
        }
        else
        {
            MonitorService.Log.Information("Article {Id} not found for update", id);
        }
        
        return updated;
    }
}