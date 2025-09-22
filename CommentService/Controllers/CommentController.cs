using System.Net;
using CommentDatabase.Models;
using CommentService.Services;
using Microsoft.AspNetCore.Mvc;
using ProfanityService.Controllers;
using Polly.CircuitBreaker;

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
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments(CancellationToken cancellationToken)
    {
        var result = await _resilienceService.GetComments(cancellationToken);
        return Ok(result);
    }
    
    [HttpPost("/profanitycheck")]
    public async Task<ActionResult<bool>> CheckForProfanity(Comment comment, CancellationToken cancellationToken)
    {
        try
        {
            var judgement = await _resilienceService.CheckForProfanity(comment, cancellationToken);
            if (judgement)
                return Ok(judgement);
            return BadRequest(new { Message = "Profanity check failed, it's present" });
        }
        catch (BrokenCircuitException brokenCircuitException)
        {
            return StatusCode(503, new
            {
                Message = "Circuit breaker has opened.",
                Detail = brokenCircuitException.Message
            });
        }
        catch (Exception ex)
        {
            return  StatusCode(500, new { Message = "An unexpected exception",  Detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostComment(string author, string content, string date, CancellationToken cancellationToken)
    {
        var newComment = await _resilienceService.PostComment(author, content, date, cancellationToken);
        if (newComment.Author != "nope")
        {
            return Ok(newComment);
        }
        
        return UnprocessableEntity("I don't know what unprocessable means, but you fucked up.");;
    }
    
    
}