using ArticleDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using PublisherService.Services;

namespace PublisherService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublisherController : Controller
{
    private readonly PublisherMessaging _publisherMessaging;

    [HttpPost]
    public async Task<IActionResult> Post(Article article)
    {
        var publisher = await PublisherMessaging.CreateAsync();
        
        if (article == null) return BadRequest();

        var result = await publisher.PublishArticle(article);
        return Ok(result);
    }
}