using Microsoft.EntityFrameworkCore;
using ProfanityDatabase.Models;

namespace ProfanityService.Services;

public class ProfanityDiService : IProfanityDiService
{
    private readonly ProfanityDbContext _profanityDbContext;
    private readonly HttpClient _httpClient;

public ProfanityDiService(ProfanityDbContext profanityDbContext, IHttpClientFactory httpClientFactory)
    {
        _profanityDbContext = profanityDbContext ?? throw new ArgumentNullException(nameof(profanityDbContext));
        _httpClient = httpClientFactory.CreateClient("ProfanityService");
    }

    public async Task<List<Profanity>> GetProfanityListAsync(CancellationToken cancellationToken)
    {
        
        return await _profanityDbContext.Profanities
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    

    public async Task<Profanity> AddProfanityAsync(Profanity profanity)
    {
        var ratifiedProfanity = _profanityDbContext.Profanities.Add(profanity);
        await _profanityDbContext.SaveChangesAsync();
        return await _profanityDbContext.Profanities.FindAsync(ratifiedProfanity.Entity.Id);
    }
}