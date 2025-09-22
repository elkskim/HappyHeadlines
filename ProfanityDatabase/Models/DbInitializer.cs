using ProfanityDatabase.Models;

namespace ProfanityDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(ProfanityDbContext context)
    {
        var presence = context.Profanities.Any();
        context.SaveChanges();
    }
}