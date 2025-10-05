using StackExchange.Redis;

namespace Monitoring;

public class CacheMetrics
{
    private readonly string _prefix;
    private readonly IDatabase _redis;

    public CacheMetrics(IConnectionMultiplexer redis, string prefix)
    {
        _redis = redis.GetDatabase();
        _prefix = prefix;
    }

    public async Task RecordHitAsync()
    {
        await _redis.StringIncrementAsync($"{_prefix}_hit");
    }

    public async Task RecordMissAsync()
    {
        await _redis.StringIncrementAsync($"{_prefix}_miss");
    }

    public async Task<long> GetHitsAsync()
    {
        return (long)await _redis.StringGetAsync($"{_prefix}:hits");
    }

    public async Task<long> GetMissesAsync()
    {
        return (long)await _redis.StringGetAsync($"{_prefix}:misses");
    }

    public async Task<double> GetHitRatio()
    {
        var hits = (double)await _redis.StringGetAsync($"{_prefix}:hits");
        var misses = (double)await _redis.StringGetAsync($"{_prefix}:misses");
        var total = hits + misses;
        return total == 0 ? 0 : hits / total;
    }

    public class ArticleCacheMetrics : CacheMetrics
    {
        public ArticleCacheMetrics(IConnectionMultiplexer redis) : base(redis, "articlecache")
        {
        }
    }

    public class CommentCacheMetrics : CacheMetrics
    {
        public CommentCacheMetrics(IConnectionMultiplexer redis) : base(redis, "commentcache")
        {
        }
    }
}