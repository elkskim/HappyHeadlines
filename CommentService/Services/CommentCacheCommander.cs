using System.Text.Json;
using CommentDatabase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Monitoring;
using StackExchange.Redis;

namespace CommentService.Services;

public class CommentCacheCommander
{
    private readonly IResilienceService _service;
    private readonly IDistributedCache _cache;
    private readonly IDatabase _redis; // StackExchange.Redis

    public CommentCacheCommander(
        IResilienceService service,
        IDistributedCache cache,
        IConnectionMultiplexer redis
)

{
        _service = service;
        _cache = cache;
        _redis = redis.GetDatabase();
    }


public async Task<IEnumerable<Comment>> GetCommentsAsync(int articleId, string region, CancellationToken ct)
    {
        var key = $"comments:{region}:{articleId}";

        // Try cache first
        var cached = await _cache.GetStringAsync(key, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            MonitorService.Log.Information("Cache hit for article {ArticleId} ({Region})", articleId, region);

            // Tried cache already
            await TouchRecentAsync(articleId, region);

            return JsonSerializer.Deserialize<IEnumerable<Comment>>(cached)!;
        }

        MonitorService.Log.Information("Cache miss for article {ArticleId} ({Region}), loading from DB", articleId, region);

        // Cache miss - load from DB

        var comments = _service.GetComments(region, articleId, ct);

        // Store in cache
        var serialized = JsonSerializer.Serialize(comments);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) // optional TTL
        }, ct);

        // Track these fellas
        await TouchRecentAsync(articleId, region);

        return await comments;
    }

    private async Task TouchRecentAsync(int articleId, string region)
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
}
