using CommentDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using ProfanityDatabase.Models;

namespace CommentService.Services;

public class ResilienceService : IResilienceService
{
    private readonly CommentDbContext _commentDbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilienceService> _logger;
    private readonly IAsyncPolicy<ProfanityCheckResult> _policy;

    public ResilienceService
    (
        CommentDbContext commentDbContext,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory iLoggerFactory
    )
    {
        _commentDbContext = commentDbContext;
        _httpClient = httpClientFactory.CreateClient();
        _logger = iLoggerFactory.CreateLogger<ResilienceService>();

        var circuitbreak = Policy
            .Handle<HttpRequestException>()
            .Or<BrokenCircuitException>()
            .CircuitBreakerAsync(
                3, // break after 3 consecutive failures
                TimeSpan.FromSeconds(30), // stay open for 30s
                (ex, breakDelay) =>
                {
                    Console.WriteLine($"Circuit opened for {breakDelay.TotalSeconds}s due to: {ex.Message}");
                },
                () => Console.WriteLine("Circuit closed again."),
                () => Console.WriteLine("Circuit in half-open state, i don't exactly know what it does, but testing...")
            );
        var fallback = Policy<ProfanityCheckResult>
            .Handle<HttpRequestException>()
            .Or<BrokenCircuitException>()
            .FallbackAsync(
                new ProfanityCheckResult(true, true),
                e =>
                {
                    Console.WriteLine($"Profanity Service unavailable - Fallback triggered {e.Exception.Message}");
                    return Task.CompletedTask;
                });
        _policy = fallback.WrapAsync(circuitbreak);

        {
        }
    }

    public async Task<IEnumerable<Comment>> GetComments(string region, int articleId, CancellationToken cancellationToken)
    {
        return await _commentDbContext.Comments.Where(c => c.Region == region && c.ArticleId == articleId).ToListAsync(cancellationToken);
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
            response.EnsureSuccessStatusCode();
            var profanities = await response.Content.ReadFromJsonAsync<List<Profanity>>(cancellationToken);

            var containsProfanity =
                profanities.Any(p => comment.Content.Contains(p.Word, StringComparison.OrdinalIgnoreCase));
            return new ProfanityCheckResult(containsProfanity, false);
        });
    }

    public async Task<ActionResult<Comment>?> PostComment(string region, [FromBody] Comment comment,
        CancellationToken cancellationToken)
    {
        await _commentDbContext.Comments.AddAsync(comment, cancellationToken);
        await _commentDbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public record ProfanityCheckResult(bool isProfane, bool serviceUnavailable);
}