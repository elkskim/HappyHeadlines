using DraftDatabase.Models;
using Microsoft.AspNetCore.Mvc;

namespace DraftService.Services;

public interface IDraftDiService
{
    Draft GetDraft(int id);
    List<Draft> GetDrafts();
    Task<Draft> CreateDraft(Draft draft);
    Task<Draft> UpdateDraft(Draft draft);
    Task<bool> DeleteDraft(int id);
}