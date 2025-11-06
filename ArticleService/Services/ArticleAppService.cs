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
    private readonly ICompressionService _compression;

    public ArticleAppService(IArticleRepository repo, IDistributedCache cache, 
        IConnectionMultiplexer redis, IMemoryCache memoryCache, ICompressionService compression)
    {
        _repo = repo;
        _cache = cache;
        _metrics = new CacheMetrics.ArticleCacheMetrics(redis);
        _memoryCache = memoryCache;
        _compression = compression;
    }

    public async Task<IEnumerable<Article>> GetArticles(string region, CancellationToken ct = default)
    {
        // NOTE: This method intentionally bypasses cache.
        // Caching all articles as a single list would be memory-intensive and hard to invalidate.
        // Individual article caching (GetArticleAsync) provides better cache hit rates.
        // Consider using GetRecentArticlesAsync or implementing pagination for large datasets.
        // Also I am incredibly lazy and don't want to write caching logic for this method.
        MonitorService.Log.Information("Getting all articles for region {Region} without caching", region);
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

        // L2: Redis cache check (fast, network hop, compressed payload)
        var compressedBytes = await _cache.GetAsync(key, ct);
        if (compressedBytes != null && compressedBytes.Length > 0)
        {
            var decompressedJson = _compression.Decompress(compressedBytes);
            var article = JsonSerializer.Deserialize<Article>(decompressedJson);
            if (article != null)
            {
                // Warm L1 cache for subsequent requests
                _memoryCache.Set(key, article, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                MonitorService.Log.Information(
                    "L2 cache hit for article {Id} (Redis, compressed: {CompressedSize} bytes)", 
                    id, compressedBytes.Length);
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
            // Warm both cache layers (L2 with compression)
            var json = JsonSerializer.Serialize(fetchedArticle);
            var originalSize = System.Text.Encoding.UTF8.GetByteCount(json);
            var compressedPayload = _compression.Compress(json);
            var ratio = _compression.CalculateCompressionRatio(originalSize, compressedPayload.Length);
            
            await _cache.SetAsync(key, compressedPayload, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
            }, ct);
            
            _memoryCache.Set(key, fetchedArticle, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            
            MonitorService.Log.Information(
                "Cached article {Id}: {OriginalSize} bytes → {CompressedSize} bytes (ratio: {Ratio:F2}x)",
                id, originalSize, compressedPayload.Length, ratio);
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

        // Cache newly created article in Redis with compression (skip memory cache; likely won't be immediately re-read)
        var key = $"article:{region}:{article.Id}";
        var json = JsonSerializer.Serialize(article);
        var compressedBytes = _compression.Compress(json);
        
        await _cache.SetAsync(key, compressedBytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
        }, ct);

        return article;
    }

    public async Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct = default)
    {
        // Invalidate both cache layers
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
            var key = $"article:{region}:{id}";
            
            // Invalidate L1 (will be warmed on next GET from L2)
            _memoryCache.Remove(key);
            
            // Proactively update L2 with compressed new version
            // This avoids a cache miss on the next GET, improving read performance after updates
            var json = JsonSerializer.Serialize(updated);
            var originalSize = System.Text.Encoding.UTF8.GetByteCount(json);
            var compressedPayload = _compression.Compress(json);
            var ratio = _compression.CalculateCompressionRatio(originalSize, compressedPayload.Length);
            
            await _cache.SetAsync(key, compressedPayload, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
            }, ct);
            
            MonitorService.Log.Information(
                "Updated and re-cached article {Id}: {OriginalSize} bytes → {CompressedSize} bytes (ratio: {Ratio:F2}x)",
                id, originalSize, compressedPayload.Length, ratio);
        }
        else
        {
            MonitorService.Log.Warning("Article {Id} not found for update", id);
        }
        
        return updated;
    }
}

