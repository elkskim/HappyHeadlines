using System.Text.Json;
using ArticleDatabase.Models;
using ArticleService.Services;
using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace ArticleService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : Controller {

    private readonly IArticleDiService  _articleDiService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ArticleController(IArticleDiService articleDiService, IHttpClientFactory httpClientFactory)
    {
        _articleDiService = articleDiService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetArticleComments(int id, [FromQuery] string region)
    {
        var client = _httpClientFactory.CreateClient("CommentsService");

        try
        {
            var response = await client.GetAsync($"{id}/comments?region={region}");

            if (!response.IsSuccessStatusCode)
            {
                MonitorService.Log.Error("Comments for article ID {id} failed with response: {Response}", id, response);
                return StatusCode(500, "We couldn't reach comments from articleservice");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            
            var comments = JsonSerializer.Deserialize<List<CommentDto>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CommentDto>();

            if (comments == null)
            {
                MonitorService.Log.Error("Comments for article ID {id} are null, with response: {Response}", id, response);
            }
            
            return Ok(comments);
            
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e);
            return StatusCode(503, $"Error contacting comments service: {e.Message}");
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, [FromQuery] string region, CancellationToken ct)
    {
        var article = await _articleDiService.GetArticleAsync(id, region, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet]
    public async Task<IEnumerable<Article>> ReadArticles([FromQuery] string region, CancellationToken ct)
    {
        return await _articleDiService.GetArticles(region, ct);
    }

    [HttpPost]
    public async Task<IActionResult> CreateArticle([FromBody] Article incArticle, [FromQuery] string region)
    {
        var article = new Article(incArticle.Title, incArticle.Content, incArticle.Author);
        await _articleDiService.CreateArticleAsync(article, region);
        return Accepted(article);
    }

     [HttpPatch("{id}")]
     public async Task<IActionResult> UpdateArticle(int id, [FromQuery] string region, [FromBody] Article updates, CancellationToken ct)
     {
         var updatedArticle = await _articleDiService.UpdateArticleAsync(id, updates, region, ct); 
         return updatedArticle is null ? NotFound() : Ok(updatedArticle);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArticle(int id, [FromQuery] string region, CancellationToken ct)
    {
        var deleted = await _articleDiService.DeleteArticleAsync(id, region, ct);
        return deleted ? NoContent() : NotFound();
    }
}