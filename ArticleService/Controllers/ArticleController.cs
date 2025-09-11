using ArticleDatabase.Models;
using Microsoft.AspNetCore.Mvc;

namespace HappyHeadlines.ArticleService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : Controller
{
    private readonly DbContextFactory _dbContextFactory;
    
    public ArticleController(DbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Article>> ReadArticles(string region)
    {
        var db = _dbContextFactory.CreateDbContext(region);
        return db.Articles;
    }

    [HttpPost]
    public async Task<IActionResult> CreateArticle(string region, string? title, string? content, string? author)
    {
        var db = _dbContextFactory.CreateDbContext(region);
        db.Articles.Add(new Article()
        {
            Title = title ?? "Default Title", 
            Content = content ?? "Default Content", 
            Author = author ??  "Default Author"
            
        });
        await db.SaveChangesAsync();
        return Accepted();
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateArticle(string region, int id, string? title, string? content, string? author)
    {
        var db = _dbContextFactory.CreateDbContext(region);
        var article = await db.Articles.FindAsync(id);
        if (article != null)
        {
            article.Title = title ?? article.Title;
            article.Content = content ?? article.Content;
            article.Author = author ?? article.Author;;
        }
        
        await db.SaveChangesAsync();
        return Accepted();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteArticle(string region, int id)
    {
        var db = _dbContextFactory.CreateDbContext(region);
        if (await db.Articles.FindAsync(id) is { } article)
        {
            db.Articles.Remove(article);
            await db.SaveChangesAsync();
            return Accepted();
        } else
        {
            return NotFound();
        }
    }
}