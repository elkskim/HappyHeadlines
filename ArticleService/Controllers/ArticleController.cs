using ArticleDatabase.Models;
using ArticleService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : Controller {

    private readonly IArticleDiService  _articleDiService;

    public ArticleController(IArticleDiService articleDiService)
    {
        _articleDiService = articleDiService;
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
        //This does not return all articles ffs
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
        //This doesnt even exist
        _articleDiService.DeleteArticle();
        
        return Accepted(article);

    }
}