using CommentDatabase.Models;
using CommentService.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using ProfanityDatabase.Models;
using ProfanityService.Controllers;

namespace CommentService.Services;

public class ResilienceService : IResilienceService
{
    private readonly CommentDbContext _commentDbContext;
    private readonly AsyncCircuitBreakerPolicy _asyncCircuitBreakerPolicy;
    private readonly HttpClient _httpClient;

    public ResilienceService
    (
        CommentDbContext commentDbContext,
        IHttpClientFactory httpClientFactory
    )
    {
        _commentDbContext = commentDbContext;
        _httpClient = httpClientFactory.CreateClient();

        {
            _asyncCircuitBreakerPolicy = Policy
                .Handle<Exception>() // TODO consider narrow exception type idiot
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
        }
    }

    public async Task<IEnumerable<Comment>> GetComments(CancellationToken cancellationToken)
    {
        return await _commentDbContext.Comments.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Profanity>> GetProfanities(CancellationToken cancellationToken)
    {
            var response = await _httpClient.GetAsync("http://localhost:5175/api/Profanity", cancellationToken);
            response.EnsureSuccessStatusCode();
            return (IEnumerable<Profanity>)response.Content.ReadFromJsonAsAsyncEnumerable<Profanity>(cancellationToken);

    }

    public async Task<bool> CheckForProfanity(Comment comment, CancellationToken cancellationToken)
    {
        
            var profanities = await GetProfanities(cancellationToken);
            profanities = profanities.ToList();
            var commentWords = comment.Content.Split(' ');
            return commentWords.Any(word => profanities.Any(prof => word == prof.Word));
    }

    public async Task<Comment> PostComment(string author, string content, string date, CancellationToken cancellationToken)
    {
        var newComment = new Comment(author, content, DateTime.Parse(date));
        if (CheckForProfanity(newComment, cancellationToken).Result) return new Comment("nope", "nope", DateTime.Now);
        await _commentDbContext.Comments.AddAsync(newComment, cancellationToken);
        await _commentDbContext.SaveChangesAsync(cancellationToken);
        return newComment;
    }
    
}