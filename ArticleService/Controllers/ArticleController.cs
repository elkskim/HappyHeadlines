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
    public async Task<IActionResult> GetArticleComments(string region, int id)
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
    public async Task<IActionResult> Get(string region, int id, CancellationToken ct)
    {
        var article = await _articleDiService.GetArticleAsync(id,region, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet]
    public async Task<IEnumerable<Article>> ReadArticles(string region, int id, CancellationToken ct)
    {
        return await _articleDiService.GetArticles(region, ct);
    }

    [HttpPost]
    public async Task<IActionResult> CreateArticle([FromBody] Article incArticle, string region)
    {
        var article = new Article(incArticle.Title, incArticle.Content, incArticle.Author);
        var db = await _articleDiService.CreateArticleAsync(article, region);
        return Accepted(article);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateArticle(string region, int id, string? title, string? content,
        string? author)
    {
        var db = _articleDiService.UpdateArticle();
        return Accepted();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteArticle(string region, int id)
    {
        var db = await _articleDiService.DeleteArticle();
        if (await _articleDiService.GetArticleAsync(id, region) is not { } article) return NotFound();
        _articleDiService.DeleteArticle();
        
        return Accepted(article);

    }
}