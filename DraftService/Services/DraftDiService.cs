using DraftDatabase.Models;
using DraftDatabase.Data;

namespace DraftService.Services;

public class DraftDiService : IDraftDiService
{
    private readonly DraftRepository _draftDbRepository;

    public DraftDiService(DraftRepository draftDbRepository)
    {
        _draftDbRepository = draftDbRepository;
    }
    
    public Draft GetDraft(int id)
    {
        return _draftDbRepository.GetById(id);
    }

    public List<Draft> GetDrafts()
    {
        return _draftDbRepository.GetDrafts().ToList();
    }

    public async Task<Draft> CreateDraft(Draft draft)
    {
        return await _draftDbRepository.Create(draft);
    }

    public async Task<Draft> UpdateDraft(Draft draft)
    {
        var oldEntity = _draftDbRepository.GetById(draft.Id);
        if (oldEntity == null) throw new Exception("Draft not found");
        await _draftDbRepository.Update(oldEntity, draft);
        return oldEntity;
    }

    public async Task<bool> DeleteDraft(int id)
    {
        var entity = _draftDbRepository.GetById(id);
        if (entity == null) return await Task.FromResult(false);
        await _draftDbRepository.Delete(entity);
        return await Task.FromResult(true);
    }
}