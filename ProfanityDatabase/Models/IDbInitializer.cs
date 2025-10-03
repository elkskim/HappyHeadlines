namespace ProfanityDatabase.Models;

public interface IDbInitializer
{
    void Initialize(ProfanityDbContext context);
}