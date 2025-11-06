using ArticleDatabase.Models;
using ArticleService.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ArticleService.Tests.Services;

/// <summary>
/// Tests for ArticleAppService - the sentinel of our two-tier caching strategy.
/// L1 (Memory): Fast but limited; 5 minutes, 100 articles.
/// L2 (Redis): Slower but persistent; 14 days, compressed with Brotli.
/// L3 (Database): The source of truth, consulted when all else fails.
/// We test that articles flow through these tiers correctly, that cache invalidation works,
/// and that our green architecture principles hold firm against the chaos.
/// </summary>
public class ArticleAppServiceTests
{
    private readonly Mock<IArticleRepository> _mockRepo;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ICompressionService> _mockCompression;
    private readonly ArticleAppService _service;

    public ArticleAppServiceTests()
    {
        _mockRepo = new Mock<IArticleRepository>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockCompression = new Mock<ICompressionService>();

        // Setup mock memory cache CreateEntry to return a mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Setup mock Redis database for CacheMetrics
        var mockDatabase = new Mock<IDatabase>();
        mockDatabase
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _mockRedis
            .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        _service = new ArticleAppService(
            _mockRepo.Object,
            _mockDistributedCache.Object,
            _mockRedis.Object,
            _mockMemoryCache.Object,
            _mockCompression.Object);
    }

    [Fact]
    public async Task GetArticleAsync_L1CacheHit_ReturnsCachedArticle()
    {
        // Arrange: An article already warming the L1 cache
        var cachedArticle = new Article("Cached in Memory", "Fast access, no network hop", "L1 Cache")
        {
            Id = 1,
            Region = "Europe"
        };

        object cacheValue = cachedArticle;
        _mockMemoryCache
            .Setup(m => m.TryGetValue("article:Europe:1", out cacheValue))
            .Returns(true);

        // Act: Retrieve from L1
        var result = await _service.GetArticleAsync(1, "Europe");

        // Assert: Should return cached article without touching L2 or database
        Assert.NotNull(result);
        Assert.Equal(cachedArticle.Title, result.Title);
        _mockDistributedCache.Verify(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.GetArticleById(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetArticleAsync_L2CacheHit_ReturnsDecompressedArticle()
    {
        // Arrange: L1 miss, but L2 (Redis) has compressed data
        var article = new Article("Cached in Redis", "Compressed payload, one network hop", "L2 Cache")
        {
            Id = 2,
            Region = "Asia"
        };

        var json = JsonSerializer.Serialize(article);
        var compressedBytes = Encoding.UTF8.GetBytes("compressed_data");

        object? nullCacheValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue("article:Asia:2", out nullCacheValue))
            .Returns(false);

        _mockDistributedCache
            .Setup(d => d.GetAsync("article:Asia:2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(compressedBytes);

        _mockCompression
            .Setup(c => c.Decompress(compressedBytes))
            .Returns(json);

        // Act: Retrieve from L2
        var result = await _service.GetArticleAsync(2, "Asia");

        // Assert: Should decompress and return article, warm L1 cache
        Assert.NotNull(result);
        Assert.Equal(article.Title, result.Title);
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once, "L1 should be warmed");
        _mockRepo.Verify(r => r.GetArticleById(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetArticleAsync_CacheMiss_FetchesFromDatabaseAndWarmsCache()
    {
        // Arrange: Both caches empty; must fetch from database
        var article = new Article("Fresh from Database", "No cache, authoritative source consulted", "Database")
        {
            Id = 3,
            Region = "Africa"
        };

        object? nullCacheValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue("article:Africa:3", out nullCacheValue))
            .Returns(false);

        _mockDistributedCache
            .Setup(d => d.GetAsync("article:Africa:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _mockRepo
            .Setup(r => r.GetArticleById(3, "Africa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        var json = JsonSerializer.Serialize(article);
        var compressedBytes = Encoding.UTF8.GetBytes("compressed_result");
        _mockCompression
            .Setup(c => c.Compress(json))
            .Returns(compressedBytes);
        _mockCompression
            .Setup(c => c.CalculateCompressionRatio(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(3.5);

        // Act: Fetch from database
        var result = await _service.GetArticleAsync(3, "Africa");

        // Assert: Should fetch from DB and warm both cache tiers
        Assert.NotNull(result);
        Assert.Equal(article.Title, result.Title);
        _mockDistributedCache.Verify(d => d.SetAsync(
            "article:Africa:3",
            compressedBytes,
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once, "L2 cache should be warmed");
        _mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once, "L1 cache should be warmed");
    }

    [Fact]
    public async Task CreateArticleAsync_CachesNewArticleInRedis()
    {
        // Arrange: A new article to be created
        var newArticle = new Article("Breaking News", "Something important happened", "Reporter")
        {
            Region = "Europe"
        };

        _mockRepo
            .Setup(r => r.AddArticleAsync(It.IsAny<Article>(), "Europe", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var json = JsonSerializer.Serialize(newArticle);
        var compressedBytes = Encoding.UTF8.GetBytes("compressed_new_article");
        _mockCompression
            .Setup(c => c.Compress(It.IsAny<string>()))
            .Returns(compressedBytes);

        // Act: Create the article
        var result = await _service.CreateArticleAsync(newArticle, "Europe");

        // Assert: Should save to DB and cache in L2 (skip L1 for new articles)
        Assert.NotNull(result);
        _mockRepo.Verify(r => r.AddArticleAsync(newArticle, "Europe", It.IsAny<CancellationToken>()), Times.Once);
        _mockDistributedCache.Verify(d => d.SetAsync(
            It.IsAny<string>(),
            compressedBytes,
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteArticleAsync_InvalidatesBothCacheTiers()
    {
        // Arrange: An article marked for deletion
        _mockRepo
            .Setup(r => r.DeleteArticleAsync(5, "Asia", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: Delete the article
        var result = await _service.DeleteArticleAsync(5, "Asia");

        // Assert: Both cache tiers must be invalidated
        Assert.True(result);
        _mockMemoryCache.Verify(m => m.Remove("article:Asia:5"), Times.Once, "L1 must be invalidated");
        _mockDistributedCache.Verify(d => d.RemoveAsync("article:Asia:5", It.IsAny<CancellationToken>()), Times.Once, "L2 must be invalidated");
        _mockRepo.Verify(r => r.DeleteArticleAsync(5, "Asia", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateArticleAsync_InvalidatesCacheAndReturnsUpdatedArticle()
    {
        // Arrange: An article requiring updates
        var updates = new Article("Updated Title", "Updated Content", "Ignored");

        var updatedArticle = new Article("Updated Title", "Updated Content", "Original Author")
        {
            Id = 7,
            Region = "Africa"
        };

        _mockRepo
            .Setup(r => r.UpdateArticleAsync(7, updates, "Africa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedArticle);

        var json = JsonSerializer.Serialize(updatedArticle);
        var compressedBytes = Encoding.UTF8.GetBytes("compressed_updated_article");
        _mockCompression
            .Setup(c => c.Compress(It.IsAny<string>()))
            .Returns(compressedBytes);
        _mockCompression
            .Setup(c => c.CalculateCompressionRatio(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(3.0);

        // Act: Update the article
        var result = await _service.UpdateArticleAsync(7, updates, "Africa");

        // Assert: L1 invalidated, L2 proactively updated with new version
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        _mockMemoryCache.Verify(m => m.Remove("article:Africa:7"), Times.Once, "L1 must be invalidated");
        _mockDistributedCache.Verify(d => d.SetAsync(
            "article:Africa:7",
            compressedBytes,
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once, "L2 should be proactively updated");
    }

    [Fact]
    public async Task GetArticles_BypassesCache_FetchesDirectlyFromDatabase()
    {
        // Arrange: Fetching all articles (no cache, too expensive to cache entire list)
        var articles = new List<Article>
        {
            new("Article 1", "Content 1", "Author 1") { Id = 1, Region = "Europe" },
            new("Article 2", "Content 2", "Author 2") { Id = 2, Region = "Europe" }
        };

        _mockRepo
            .Setup(r => r.GetAllArticles("Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        // Act: Get all articles
        var result = await _service.GetArticles("Europe");

        // Assert: Should bypass cache entirely
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockMemoryCache.Verify(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny), Times.Never);
        _mockDistributedCache.Verify(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecentArticlesAsync_FetchesArticlesSinceDate()
    {
        // Arrange: Fetching recent articles (last 14 days)
        var recentArticles = new List<Article>
        {
            new("Recent Article", "Content", "Author") { Id = 10, Region = "Asia", Created = DateTime.UtcNow.AddDays(-7) }
        };

        _mockRepo
            .Setup(r => r.GetRecentArticlesAsync("Asia", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentArticles);

        // Act: Get recent articles
        var result = await _service.GetRecentArticlesAsync("Asia");

        // Assert: Should fetch from repository
        Assert.NotNull(result);
        Assert.Single(result);
        _mockRepo.Verify(r => r.GetRecentArticlesAsync(
            "Asia",
            It.Is<DateTime>(d => d <= DateTime.UtcNow.AddDays(-14)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
