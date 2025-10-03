using DraftDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Monitoring;

namespace DraftDatabase.Data;

public class DraftRepository : IDraftRepository
{
    private readonly DraftDbContext _db;

    public DraftRepository(DraftDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Draft>? GetDrafts()
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftRepository.GetDrafts");
        MonitorService.Log.Information("Fetching drafts");
        if (_db.Drafts.Any())
        {
            MonitorService.Log.Information("Non-zero drafts fetched - returning");
            return _db.Drafts;
        }

        ;
        MonitorService.Log.Information("No drafts found - returning null");
        return null;
    }

    public Draft? GetById(int id)
    {
        return _db.Drafts.Find(id) ?? null;
    }

    public async Task<Draft> Create(Draft entity)
    {
        entity.Created = DateTime.Now;
        await _db.AddAsync(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<Draft> Update(Draft oldEntity, Draft newEntity)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftRepository.Update");
        MonitorService.Log.Information("Updating Draft ID {id}", oldEntity.Id);
        var rowsAffected = await _db.Drafts
            .Where(d => d.Id == newEntity.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Author, newEntity.Author)
                .SetProperty(e => e.Title, newEntity.Title));

        oldEntity = (await _db.Drafts.FindAsync(newEntity.Id))!;
        if (rowsAffected == 0)
        {
            MonitorService.Log.Error("Update Failed for Draft ID {id}", oldEntity.Id);
            return null;
        }

        MonitorService.Log.Information("Draft ID {id} updated", oldEntity.Id);
        return oldEntity;
    }

    public async Task<bool> Delete(Draft entity)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("DraftRepository.Delete");
        MonitorService.Log.Information("Deleting Draft ID {id}", entity.Id);
        var rowsAffected = await _db.Drafts.Where(e => e.Id == entity.Id).ExecuteDeleteAsync();
        if (rowsAffected == 0)
        {
            MonitorService.Log.Error("Delete Failed for Draft ID {id} - Rows Unaffected", entity.Id);
            return false;
        }

        MonitorService.Log.Information("Draft ID {id} deleted", entity.Id);
        return await _db.Drafts.FindAsync(entity.Id) == null;
    }
}