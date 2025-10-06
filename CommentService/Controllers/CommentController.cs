using CommentDatabase.Models;
using CommentService.Services;
using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace CommentService.Controllers;

[Route("/api/[controller]")]
[ApiController]
public class CommentController : Controller
{
    private readonly IResilienceService _resilienceService;

    public CommentController(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments(string region, int articleId, CancellationToken cancellationToken)
    {
        var result = await _resilienceService.GetComments(region, articleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetCommentById(int articleId, string region, int commentId, CancellationToken cancellationToken)
    {
        var comment = await _resilienceService.GetCommentById(articleId, region, commentId, cancellationToken);
        if (comment == null) return NotFound($"Comment with id {commentId} not found");

        return Ok(comment);
    }

    [HttpPost("/profanitycheck")]
    public ActionResult CheckForProfanity([FromBody] Comment comment,
        CancellationToken cancellationToken)
    {
        return StatusCode(500,
            new { Message = "An unexpected exception - you tried to access profanitycheck manually. Idiot." });
    }

    [HttpPost]
    public async Task<IActionResult> PostComment([FromBody]Comment? comment, CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Requesting postcomment");
        if (comment == null) return BadRequest("Comment is empty, idiot");

        var judgement = await _resilienceService.CheckForProfanity(comment, cancellationToken);
        if (judgement.serviceUnavailable)
        {
            MonitorService.Log.Error("Profanity service cannot be reached.");
            return StatusCode(503, "Profanity service currently unavailable");
        }

        if (judgement.isProfane)
        {
            MonitorService.Log.Warning("Comment contained profanity. You have been denied.");
            return BadRequest("The comment contains profanity. You're out.");
        }

        var confirmedComment = await _resilienceService.PostComment(comment, cancellationToken);
        MonitorService.Log.Information("Commented posted succesfully.");
        return CreatedAtAction(
            nameof(GetCommentById),
            new { id = comment.Id },
            confirmedComment
        );
    }
}