using ProfanityDatabase.Models;

namespace ProfanityDatabase.Models;

public class DbInitializer : IDbInitializer
{
    public void Initialize(ProfanityDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Profanities.Any()) return;
        context.Profanities.Add(new Profanity { Word = "shit" });
        context.Profanities.Add(new Profanity { Word = "fuck" });
        context.Profanities.Add(new Profanity { Word = "piss" });
        context.Profanities.Add(new Profanity { Word = "ouioiouiouhidiot" });
        
        context.SaveChanges();
    }
}