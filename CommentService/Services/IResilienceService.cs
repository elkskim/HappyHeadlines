using CommentDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using ProfanityDatabase.Models;

namespace CommentService.Services;

public interface IResilienceService
{
    Task<ResilienceService.ProfanityCheckResult>
        CheckForProfanity(Comment comment, CancellationToken cancellationToken);

    Task<ActionResult<Comment>?> PostComment(string region, [FromBody] Comment comment,
        CancellationToken cancellationToken);
    Task<IEnumerable<Profanity>> GetProfanities(CancellationToken cancellationToken);
    Task<IEnumerable<Comment>> GetComments(string region, int articleId, CancellationToken cancellationToken);
    Task<Comment?> GetCommentById(int articleId, string region, int commentId, CancellationToken cancellationToken);
}