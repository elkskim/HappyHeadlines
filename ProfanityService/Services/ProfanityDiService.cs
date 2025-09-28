using Microsoft.EntityFrameworkCore;
using ProfanityDatabase.Models;

namespace ProfanityService.Services;

public class ProfanityDiService : IProfanityDiService
{
    private readonly ProfanityDbContext _profanityDbContext;

    public ProfanityDiService(ProfanityDbContext profanityDbContext)
    {
        _profanityDbContext = profanityDbContext ?? throw new ArgumentNullException(nameof(profanityDbContext));
    }
    
    public async Task<List<Profanity>> GetProfanityListAsync()
    {
        return await _profanityDbContext.Profanities.ToListAsync();
    }

    public async Task<Profanity> AddProfanityAsync(Profanity profanity)
    {
        var ratifiedProfanity = _profanityDbContext.Profanities.Add(profanity);
        await _profanityDbContext.SaveChangesAsync();
        return await _profanityDbContext.Profanities.FindAsync(ratifiedProfanity.Entity.Id);

    }
}