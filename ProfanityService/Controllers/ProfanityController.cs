using Microsoft.AspNetCore.Mvc;
using ProfanityDatabase.Models;

namespace ProfanityService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProfanityController : Controller
{
    private readonly ProfanityDbContextFactory _context;

    public ProfanityController(ProfanityDbContextFactory context)
    {
        _context = context;
    }
    
    [HttpGet]
    public List<Profanity> GetProfanities()
    {
        var db = _context.CreateDbContext([""]);
        return db.Profanities.ToList();
    }

    [HttpPost]
    public async Task<Profanity> CreateProfanity(string word)
    {
        var db = _context.CreateDbContext([""]);
        var newProfanity = new Profanity { Word = word } ;
        await db.Profanities.AddAsync(newProfanity);
        await db.SaveChangesAsync();
        return newProfanity;
    }
}