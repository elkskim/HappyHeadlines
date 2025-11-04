using Microsoft.Extensions.Caching.Distributed;
using Monitoring;
using StackExchange.Redis;

namespace ArticleService.Services;

public class ArticleCacheCommander : BackgroundService
{
    private readonly IDistributedCache _cache;
    private readonly IEnumerable<string> _regions;
    private readonly IArticleAppService _service;

    public ArticleCacheCommander(IArticleAppService service, IDistributedCache cache, IConfiguration config)
    {
        _service = service;
        _cache = cache;

        _regions = config.GetSection("ConnectionStrings")
            .GetChildren()
            .Select(cs => cs.Key)
            .Where(region => !region.Contains("Redis") && !region.Contains("Host"))
            .ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            foreach (var region in _regions)

                try
                {
                    MonitorService.Log.Information("Sticking the last 14 days of articles in the cache for {Region}", region);

                    var recentArticles = await _service.GetRecentArticlesAsync(region, stoppingToken);

                    foreach (var article in recentArticles)
                        await _service.GetArticleAsync(article.Id, region, stoppingToken);


                }

                catch (Exception ex)

                {
                    MonitorService.Log.Error(ex, "Failed to refresh article cache for {Region}", region);
                }
            
            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            
        }

        
    }
}

