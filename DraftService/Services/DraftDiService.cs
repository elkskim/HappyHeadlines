using DraftDatabase.Data;
using DraftDatabase.Models;
using Monitoring;

namespace DraftService.Services;

public class DraftDiService : IDraftDiService
{
    private readonly IDraftRepository _draftDbRepository;


    public DraftDiService(IDraftRepository draftDbRepository)
    {
        _draftDbRepository = draftDbRepository;
    }

    public Draft? GetDraftById(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftDiService.GetDraftById");
        MonitorService.Log.Information("Processing Business Logic - GetDraftById");
        return _draftDbRepository.GetById(id);
    }

    public List<Draft>? GetDrafts()
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftDiService.GetDrafts");
        MonitorService.Log.Information("Processing Business Logic - GetDrafts");
        var drafts = _draftDbRepository.GetDrafts();
        if (drafts == null)
        {
            MonitorService.Log.Information("No drafts found - returning null");
            return null;
        }

        MonitorService.Log.Information("Returning drafts");
        return drafts.ToList();
    }

    public async Task<Draft> CreateDraft(Draft draft)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftDiService.CreateDraft");
        MonitorService.Log.Information("Processing Business Logic - CreateDraft");
        var newDraft = await _draftDbRepository.Create(draft);
        MonitorService.Log.Information("New Draft with ID {id} - Created", newDraft.Id);
        return newDraft;
    }

    public async Task<Draft> UpdateDraft(Draft draft)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftDiService.UpdateDraft");
        MonitorService.Log.Information("Processing Business Logic - UpdateDraft");
        var oldEntity = _draftDbRepository.GetById(draft.Id);
        if (oldEntity == null)
        {
            MonitorService.Log.Warning("Draft with ID {id} Not found - Returning null", draft.Id);
            return null;
        }

        MonitorService.Log.Information("Entity Acquired - Updating");
        var updatedEntity = await _draftDbRepository.Update(oldEntity, draft);
        MonitorService.Log.Information("Entity Returned by Repository - Updated");
        return updatedEntity;
    }

    public async Task<bool> DeleteDraft(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftDiService.DeleteDraft");
        MonitorService.Log.Information("Processing Business Logic - DeleteDraft");
        var entity = _draftDbRepository.GetById(id);
        if (entity == null)
        {
            MonitorService.Log.Warning("Draft with ID {id} Not found", id);
            return await Task.FromResult(false);
        }

        MonitorService.Log.Information("Entity Acquired - Deleting");
        await _draftDbRepository.Delete(entity);
        return await Task.FromResult(true);
    }
}