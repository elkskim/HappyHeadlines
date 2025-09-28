using DraftDatabase.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftDatabase.Data;

public class DraftRepository : IDraftRepository
{
    private readonly DraftDbContext _db;

    public DraftRepository(DraftDbContext db)
    {
        _db = db;
    }
    
    public IEnumerable<Draft> GetDrafts()
    {
        return _db.Drafts;
    }

    public Draft? GetById(int id)
    {
        return _db.Drafts.Find(id) ??  null;
    }

    public async Task<Draft> Create(Draft entity)
    {
        await _db.AddAsync(entity);
        await _db.SaveChangesAsync();
        return entity;
        
    }
    public async Task<Draft> Update(Draft oldEntity, Draft newEntity)
    {
        var rowsAffected = await _db.Drafts
            .Where(d => d.Id == newEntity.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Author, newEntity.Author)
                .SetProperty(e => e.Title, newEntity.Title));
        
        oldEntity = (await _db.Drafts.FindAsync(oldEntity.Id))!;
        return rowsAffected == 0 ? throw new Exception("Update failed - No rows affected") : oldEntity;
    }

    public async Task<bool> Delete(Draft entity)
    {
        await  _db.Drafts.Where(e => e.Id == entity.Id).ExecuteDeleteAsync();
        return (await _db.Drafts.FindAsync(entity.Id)) == null;;
    }
}