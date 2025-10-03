using DraftDatabase.Models;

namespace DraftService.Services;

public interface IDraftDiService
{
    Draft? GetDraftById(int id);
    List<Draft>? GetDrafts();
    Task<Draft> CreateDraft(Draft draft);
    Task<Draft> UpdateDraft(Draft draft);
    Task<bool> DeleteDraft(int id);
}