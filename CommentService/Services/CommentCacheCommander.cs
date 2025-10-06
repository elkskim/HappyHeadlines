using System.Text.Json;
using CommentDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Monitoring;
using StackExchange.Redis;

namespace CommentService.Services;

public class CommentCacheCommander : ICommentCacheCommander
{
    private readonly CommentDbContext _commentDbContext;
    private readonly IDistributedCache _cache;
    private readonly IDatabase _redis;
    private readonly CacheMetrics _metrics;

    public CommentCacheCommander(
        CommentDbContext commentDbContext,
        IDistributedCache cache,
        IConnectionMultiplexer redis
)

{
        _commentDbContext = commentDbContext;
        _cache = cache;
        _redis = redis.GetDatabase();
        _metrics = new CacheMetrics(redis, "commentcache");
}


public async Task<IEnumerable<Comment>> GetCommentsAsync(int articleId, string region, CancellationToken ct)
    {
        var key = $"comments:{region}:{articleId}";

        // Try cache first
        var cached = await _cache.GetStringAsync(key, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            await _metrics.RecordHitAsync();
            MonitorService.Log.Information("Cache hit for article {ArticleId} ({Region})", articleId, region);
            MonitorService.Log.Information("The actual eggs in cache: {ArticleId} ({Region}): {Cached}", articleId, region, cached);
            
            // Tried cache already
            await TouchRecentAsync(articleId, region);

            var result = JsonSerializer.Deserialize<IEnumerable<Comment>>(cached)!;
            return result;
        }
        
        await _metrics.RecordMissAsync();
        MonitorService.Log.Information("Cache miss for article {ArticleId} ({Region}), loading from DB", articleId, region);
        
        // Cache miss - load from DB

        var comments = await _commentDbContext.Comments.Where(x => x.ArticleId == articleId && x.Region == region).ToListAsync(cancellationToken: ct);

        // Store in cache
        var serialized = JsonSerializer.Serialize(comments);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) // optional TTL
        }, ct);

        // Track these fellas
        await TouchRecentAsync(articleId, region);

        return comments;
    }

    public async Task TouchRecentAsync(int articleId, string region)
    {
        var zsetKey = $"comments:recent:{region}";

        // Add or update with timestamp
        await _redis.SortedSetAddAsync(zsetKey, articleId.ToString(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Trim down to last 30
        var count = await _redis.SortedSetLengthAsync(zsetKey);
        if (count > 30)
        {
            await _redis.SortedSetRemoveRangeByRankAsync(zsetKey, 0, count - 31);
        }
    }
    
    /*
     * Cache won't properly populate with new comments? kill it
     */
    public async Task InvalidateCommentsCacheAsync(int articleId, string region, CancellationToken ct)
    {
        var key = $"comments:{region}:{articleId}";

        await _cache.RemoveAsync(key, ct);
        MonitorService.Log.Information("Cache invalidated for article {ArticleId} ({Region})", articleId, region);
    }
}
