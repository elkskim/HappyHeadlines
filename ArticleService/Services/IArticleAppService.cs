using ArticleDatabase.Models;

namespace ArticleService.Services;

/// <summary>
/// Application service for Article operations with two-tier caching (memory + Redis).
/// Implements Green Software Foundation's "Fetch Data from Proximity" pattern.
/// </summary>
public interface IArticleAppService
{
    /// <summary>
    /// Retrieves all articles for a given region.
    /// </summary>
    /// <param name="region">The region to fetch articles from (e.g., "Europe", "Global")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of articles</returns>
    Task<IEnumerable<Article>> GetArticles(string region, CancellationToken ct);
    
    /// <summary>
    /// Retrieves a single article by ID with two-tier caching.
    /// Cache cascade: L1 (memory, 5min) → L2 (Redis, 14d) → L3 (database).
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <param name="region">Region the article belongs to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Article if found, null otherwise</returns>
    Task<Article?> GetArticleAsync(int id, string region, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves articles created within the last 14 days for a region.
    /// </summary>
    /// <param name="region">Region to fetch recent articles from</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of recent articles</returns>
    Task<List<Article>> GetRecentArticlesAsync(string region, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a new article and caches it in Redis.
    /// </summary>
    /// <param name="article">Article to create</param>
    /// <param name="region">Region to store the article in</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created article with ID assigned</returns>
    Task<Article> CreateArticleAsync(Article article, string region, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes an article and invalidates both cache layers (memory + Redis).
    /// </summary>
    /// <param name="id">Article ID to delete</param>
    /// <param name="region">Region the article belongs to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteArticleAsync(int id, string region, CancellationToken ct = default);
    
    /// <summary>
    /// Updates an article and invalidates both cache layers (memory + Redis).
    /// </summary>
    /// <param name="id">Article ID to update</param>
    /// <param name="updates">Article with updated properties</param>
    /// <param name="region">Region the article belongs to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated article if found, null otherwise</returns>
    Task<Article?> UpdateArticleAsync(int id, Article updates, string region, CancellationToken ct = default);
}

