using CommentDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly;
using Polly.CircuitBreaker;
using ProfanityDatabase.Models;

namespace CommentService.Services;

public class ResilienceService : IResilienceService
{
    private readonly CommentDbContext _commentDbContext;
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<ProfanityCheckResult> _policy;
    private readonly ICommentCacheCommander _commentCacheCommander;
    public ResilienceService
    (
        CommentDbContext commentDbContext,
        IHttpClientFactory httpClientFactory,
        ICommentCacheCommander commentCacheCommander
    )
    {
        _commentDbContext = commentDbContext;
        _httpClient = httpClientFactory.CreateClient("profanity");
        _commentCacheCommander = commentCacheCommander;
        
        // Circuit breaker configuration: Breaks after 3 consecutive HttpRequestExceptions
        // EnsureSuccessStatusCode() throws HttpRequestException on non-success responses (404, 500, etc.)
        // After 3 consecutive failures, circuit opens for 30 seconds and fast-fails without HTTP calls
        // This prevents cascading failures when ProfanityService is down or overloaded
        var circuitbreak = Policy
            .Handle<HttpRequestException>()
            .Or<BrokenCircuitException>()
            .CircuitBreakerAsync(
                3, // break after 3 consecutive failures
                TimeSpan.FromSeconds(30), // stay open for 30s
                (ex, breakDelay) =>
                {
                    MonitorService.Log.Error(ex, "Circuit breaker OPENED for {Delay}s - ProfanityService failing", breakDelay.TotalSeconds);
                },
                () => MonitorService.Log.Information("Circuit breaker CLOSED - ProfanityService recovered"),
                () => MonitorService.Log.Warning("Circuit breaker HALF-OPEN - Testing ProfanityService recovery")
            );
        var fallback = Policy<ProfanityCheckResult>
            .Handle<HttpRequestException>()
            .Or<BrokenCircuitException>()
            .FallbackAsync(
                new ProfanityCheckResult(true, true), // Fail closed: treat as profane when service unavailable
                e =>
                {
                    MonitorService.Log.Warning(e.Exception, "Fallback triggered - ProfanityService unavailable, blocking comment");
                    return Task.CompletedTask;
                });
        _policy = fallback.WrapAsync(circuitbreak);

        {
        }
    }

    public async Task<IEnumerable<Comment>> GetComments(string region, int articleId, CancellationToken cancellationToken)
    {
       // return await _commentDbContext.Comments.Where(c => c.Region == region && c.ArticleId == articleId).ToListAsync(cancellationToken);
       return await _commentCacheCommander.GetCommentsAsync(articleId, region, cancellationToken);
    }

    public async Task<Comment?> GetCommentById(int articleId, string region, int commentId, CancellationToken cancellationToken)
    {
        var result = await _commentDbContext.Comments.Where(
            c => 
                c.Region == region && 
                c.ArticleId == articleId &&
                c.Id == commentId).ElementAtAsync(0, cancellationToken);
        return result;
    }

    public async Task<IEnumerable<Profanity>> GetProfanities(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("http://profanity-service:80/api/Profanity", cancellationToken);
        var profanities = await response.Content.ReadFromJsonAsync<IEnumerable<Profanity>>(cancellationToken);
        return profanities ?? [];
    }

    public async Task<ProfanityCheckResult> CheckForProfanity(Comment comment, CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async () =>
        {
            var response =
                await _httpClient.GetAsync("http://profanity-service:80/api/Profanity", cancellationToken);
            
            // Throw exception on non-success status codes so circuit breaker can count failures
            // After 3 consecutive failures, circuit opens and fast-fails without HTTP call
            response.EnsureSuccessStatusCode();
            
            var profanities = await response.Content.ReadFromJsonAsync<List<Profanity>>(cancellationToken);

            var containsProfanity =
                profanities.Any(p => comment.Content.Contains(p.Word, StringComparison.OrdinalIgnoreCase));
            return new ProfanityCheckResult(containsProfanity, false);
        });
    }

    public async Task<ActionResult<Comment>?> PostComment([FromBody] Comment comment,
        CancellationToken cancellationToken)
    {
        await _commentDbContext.Comments.AddAsync(comment, cancellationToken);
        await _commentDbContext.SaveChangesAsync(cancellationToken);
        
        //This should get the cache out and back again
        await _commentCacheCommander.InvalidateCommentsCacheAsync(comment.ArticleId, comment.Region, cancellationToken);
        return comment;
    }

    public record ProfanityCheckResult(bool isProfane, bool serviceUnavailable);
}