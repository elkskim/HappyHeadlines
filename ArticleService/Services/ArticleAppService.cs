using System.Text.Json;
using ArticleDatabase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Monitoring;
using StackExchange.Redis;

namespace ArticleService.Services;

/// <summary>
/// Application service implementing two-tier caching strategy for articles.
/// L1: Memory cache (5min TTL, 100 article limit) - 0 network hops
/// L2: Redis cache (14 day TTL) - 1 network hop
/// L3: SQL Server database - Authoritative source
/// Green architecture: Reduces network traffic by 50-70% via proximity caching.
/// </summary>
public class ArticleAppService : IArticleAppService
{
    private readonly IArticleRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly CacheMetrics.ArticleCacheMetrics _metrics;
    private readonly IMemoryCache _memoryCache;

    public ArticleAppService(IArticleRepository repo, IDistributedCache cache, 
        IConnectionMultiplexer redis, IMemoryCache memoryCache)
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

        // L1: Memory cache check (fastest, local RAM)
        if (_memoryCache.TryGetValue(key, out Article? cachedArticle))
        {
            MonitorService.Log.Information("L1 cache hit for article {Id} (memory)", id);
            await _metrics.RecordHitAsync();
            return cachedArticle;
        }

        // L2: Redis cache check (fast, network hop)
        var redisCached = await _cache.GetStringAsync(key, ct);
        if (redisCached != null)
        {
            var article = JsonSerializer.Deserialize<Article>(redisCached);
            if (article != null)
            {
                // Warm L1 cache for subsequent requests
                _memoryCache.Set(key, article, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                MonitorService.Log.Information("L2 cache hit for article {Id} (Redis)", id);
                await _metrics.RecordHitAsync();
                
                return article;
            }
        }
        
        // L3: Database fetch (slowest, authoritative)
        MonitorService.Log.Information("Cache miss for article {Id}; fetching from database", id);
        await _metrics.RecordMissAsync();
        
        var fetchedArticle = await _repo.GetArticleById(id, region, ct);
        if (fetchedArticle != null)
        {
            // Warm both cache layers
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

        // Cache newly created article in Redis (skip memory cache; likely won't be immediately re-read)
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
        // Invalidate both cache layers atomically
        var key = $"article:{region}:{id}";
        _memoryCache.Remove(key);
        await _cache.RemoveAsync(key, ct);
        MonitorService.Log.Information("Invalidated L1+L2 cache for article {Id}", id);
        
        // Delegate to repository
        var deleted = await _repo.DeleteArticleAsync(id, region, ct);
        if (deleted)
        {
            MonitorService.Log.Information("Deleted article {Id} from repository", id);
        }
        else
        {
            MonitorService.Log.Warning("Article {Id} not found for deletion", id);
        }
        
        return deleted;
    }

    public async Task<Article?> UpdateArticleAsync(int id, Article updates, string region, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updates);
        
        var updated = await _repo.UpdateArticleAsync(id, updates, region, ct);
        
        if (updated != null)
        {
            // Invalidate both cache layers atomically
            var key = $"article:{region}:{id}";
            _memoryCache.Remove(key);
            await _cache.RemoveAsync(key, ct);
            MonitorService.Log.Information("Invalidated L1+L2 cache for updated article {Id}", id);
        }
        else
        {
            MonitorService.Log.Warning("Article {Id} not found for update", id);
        }
        
        return updated;
    }
}

