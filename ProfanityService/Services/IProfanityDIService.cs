using ProfanityDatabase.Models;

namespace ProfanityService.Services;

public interface IProfanityDiService
{
    Task<List<Profanity>> GetProfanityListAsync();
    Task<Profanity> AddProfanityAsync(Profanity profanity);
}