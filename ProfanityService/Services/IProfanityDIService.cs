using ProfanityDatabase.Models;

namespace ProfanityService.Services;

public interface IProfanityDiService
{
    Task<List<Profanity>> GetProfanityListAsync(CancellationToken cancellationToken);
    Task<Profanity> AddProfanityAsync(Profanity profanity);
}