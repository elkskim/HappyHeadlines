using System.Globalization;
using System.Net;
using CommentDatabase.Models;
using CommentService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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

    [HttpGet("{id}")]
    public async Task<ActionResult> GetCommentById(int commentId, CancellationToken cancellationToken)
    {
        var comment = await _resilienceService.GetCommentById(commentId, cancellationToken);
        if (comment == null)
        {
            return NotFound($"Comment with id {commentId} not found");
        }
        
        return Ok(comment);
    }
    
    [HttpPost("/profanitycheck")]
    public async Task<ActionResult<bool>> CheckForProfanity([FromBody] Comment comment, CancellationToken cancellationToken)
    {
        
            return  StatusCode(500, new { Message = "An unexpected exception - you tried to access profanitycheck manually. Idiot." });
        
    }

    [HttpPost]
    //TODO I SWEAR TO FUCKING GOD
    public async Task<IActionResult> PostComment([FromBody] Comment? comment, CancellationToken cancellationToken)
    {
        if (comment == null)
        {
            return BadRequest("Comment is empty, idiot");
        }
        
        var judgement = await _resilienceService.CheckForProfanity(comment, cancellationToken);
        if (judgement.serviceUnavailable) return StatusCode(503, "Profanity service currently unavailable");
        if (judgement.isProfane) return BadRequest("The comment contains profanity. You're out.");
        
        //TODO THIS SEEMS TO CAUSE A FUCKING ISSUE
        var confirmedComment =  await _resilienceService.PostComment(comment, cancellationToken);
        
            return CreatedAtAction(
                nameof(GetCommentById),
                new { id = comment.Id },
                confirmedComment
            );
        
        
    }
    
    
}