using CommentDatabase.Models;

namespace CommentService.Services;

public interface ICommentCacheCommander
{
    Task<IEnumerable<Comment>> GetCommentsAsync(int articleId, string region, CancellationToken ct);
    Task TouchRecentAsync(int articleId, string region);
    Task InvalidateCommentsCacheAsync(int articleId, string region, CancellationToken ct);
}