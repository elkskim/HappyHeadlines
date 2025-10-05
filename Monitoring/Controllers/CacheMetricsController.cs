using Microsoft.AspNetCore.Mvc;

namespace Monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheMetricsController : ControllerBase
{
    private readonly CacheMetrics.ArticleCacheMetrics _articleMetrics;
    private readonly CacheMetrics.CommentCacheMetrics _commentMetrics;

    public CacheMetricsController(CacheMetrics.ArticleCacheMetrics articleMetrics,
        CacheMetrics.CommentCacheMetrics commentMetrics)
    {
        _articleMetrics = articleMetrics;
        _commentMetrics = commentMetrics;
    }

    //imagine that this could work and maybe it will
    [HttpGet("cache")]
    public async Task<IActionResult> GetCacheMetrics()
    {
        var articleRatio = await _articleMetrics.GetHitRatio();
        var commentRatio = await _commentMetrics.GetHitRatio();

        return Ok(new
        {
            ArticleCacheHitRatio = articleRatio,
            CommentCacheHitRatio = commentRatio,
            ArticleCacheHits = await _articleMetrics.GetHitsAsync(),
            CommentCacheHits = await _commentMetrics.GetHitsAsync(),
            Timestamp = DateTime.UtcNow
        });

        //jesus christ
    }
}