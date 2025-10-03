using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProfanityDatabase.Models;
using ProfanityService.Services;

namespace ProfanityService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProfanityController : Controller
{
    private readonly IProfanityDiService _profanityDiService;

    public ProfanityController(IProfanityDiService profanityDiService)
    {
        _profanityDiService = profanityDiService;
    }

    [HttpGet]
    public async Task<List<Profanity>> GetProfanities()
    {
        return await _profanityDiService.GetProfanityListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Profanity>> CreateProfanity([FromBody] Profanity profanity)
    {
        profanity.Word = profanity.Word.ToLower();
        if (string.IsNullOrWhiteSpace(profanity.Word))
            return BadRequest("What did you think? Empty Space won't work at all!");
        if (!(await GetProfanities()).Where(p => p.Word.ToLower() == profanity.Word).IsNullOrEmpty())
            return BadRequest($"Profanity database already contains {profanity.Word}");
        return Ok(await _profanityDiService.AddProfanityAsync(profanity));
    }
}