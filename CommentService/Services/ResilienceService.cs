using System.Net.Sockets;
using CommentDatabase.Models;
using CommentService.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.CircuitBreaker;
using ProfanityDatabase.Models;
using ProfanityService.Controllers;

namespace CommentService.Services;

public class ResilienceService : IResilienceService
{
    private readonly CommentDbContext _commentDbContext;
    private readonly IAsyncPolicy<ProfanityCheckResult> _policy;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilienceService> _logger;
    
    public record ProfanityCheckResult(bool isProfane, bool serviceUnavailable);

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
                exceptionsAllowedBeforeBreaking: 3, // break after 3 consecutive failures
                durationOfBreak: TimeSpan.FromSeconds(30), // stay open for 30s
                onBreak: (ex, breakDelay) =>
                {
                    Console.WriteLine($"Circuit opened for {breakDelay.TotalSeconds}s due to: {ex.Message}");
                },
                onReset: () => Console.WriteLine("Circuit closed again."),
                onHalfOpen: () => Console.WriteLine("Circuit in half-open state, i don't exactly know what it does, but testing...")
            );
        var fallback = Policy<ProfanityCheckResult>
            .Handle<HttpRequestException>()
            .Or<BrokenCircuitException>()
            .FallbackAsync(
                fallbackValue: new ProfanityCheckResult(isProfane: true, serviceUnavailable: true),
                onFallbackAsync: e =>
                {
                    Console.WriteLine($"Profanity Service unavailable - Fallback triggered {e.Exception.Message}");
                    return Task.CompletedTask;
                });
        _policy = fallback.WrapAsync(circuitbreak);

        {
            
        }
    }

    public async Task<IEnumerable<Comment>> GetComments(CancellationToken cancellationToken)
    {
        return await _commentDbContext.Comments.ToListAsync(cancellationToken);
    }

    public async Task<Comment?> GetCommentById(int commentId, CancellationToken cancellationToken)
    {
        return await _commentDbContext.Comments.FindAsync([commentId, cancellationToken], cancellationToken: cancellationToken);
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

                var containsProfanity = profanities.Any(p => comment.Content.Contains(p.Word, StringComparison.OrdinalIgnoreCase));
                return new ProfanityCheckResult(containsProfanity, serviceUnavailable: false);
            });
        }
    
    public async Task<ActionResult<Comment>?> PostComment([FromBody]Comment comment, CancellationToken cancellationToken)
    {
        await _commentDbContext.Comments.AddAsync(comment, cancellationToken);
        await _commentDbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }
    
}