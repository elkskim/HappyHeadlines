using CommentDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using ProfanityDatabase.Models;

namespace CommentService.Services;

public interface IResilienceService
{
    Task<bool> CheckForProfanity(Comment comment, CancellationToken cancellationToken);
    Task<Comment> PostComment(string author, string content, string date, CancellationToken cancellationToken);
    Task<IEnumerable<Profanity>> GetProfanities(CancellationToken cancellationToken);
    Task<IEnumerable<Comment>> GetComments(CancellationToken cancellationToken);
}