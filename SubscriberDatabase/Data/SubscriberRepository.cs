using Microsoft.EntityFrameworkCore;
using Monitoring;
using SubscriberDatabase.Model;

namespace SubscriberDatabase.Data;

public class SubscriberRepository : ISubscriberRepository
{
    private readonly SubscriberDbContext _context;

    public SubscriberRepository(SubscriberDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Subscriber>> GetSubscribersAsync()
    {
        MonitorService.Log.Information("SubscriberRepository.GetSubscribers() called.");
        return await _context.Subscribers.ToListAsync();
    }

    public async Task<Subscriber?> GetByIdAsync(int id)
    {
        MonitorService.Log.Information("SubscriberRepository.GetById called.");
        return await _context.Subscribers.FindAsync(id);
    }

    public async Task<Subscriber> CreateAsync(Subscriber entity)
    {
        MonitorService.Log.Information("SubscriberRepository.Create() called.");
        await _context.Subscribers.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Subscriber?> UpdateAsync(int id, Subscriber newEntity)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberRepository.Update");
        var oldEntity = await _context.Subscribers.FindAsync(id);
        if (oldEntity == null)
        {
            MonitorService.Log.Warning("Subscriber {Id} not found for update", id);
            return null;
        }
        MonitorService.Log.Information("Updating Subscriber at ID {id}", oldEntity.Id);
        var rowsAffected = await _context.Subscribers
            .Where(d => d.Id == newEntity.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.UserId, newEntity.UserId)
                .SetProperty(e => e.Region, newEntity.Region)
                .SetProperty(e => e.Email, newEntity.Email)
                .SetProperty(e => e.SubscribedOn, newEntity.SubscribedOn));

        oldEntity = (await _context.Subscribers.FindAsync(newEntity.Id))!;
        if (rowsAffected == 0)
        {
            MonitorService.Log.Error("Update Failed for Subscriber at ID {id}", oldEntity.Id);
            return null;
        }

        MonitorService.Log.Information("Subscriber at ID {id} updated", oldEntity.Id);
        return oldEntity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberRepository.Delete");
        var entity = await _context.Subscribers.FindAsync(id);
        if (entity == null)
        {
            MonitorService.Log.Warning("Subscriber {Id} not found for deletion", id);
            return false;
        }
        MonitorService.Log.Information("Deleting Subscriber at ID {id}", entity.Id);
        var rowsAffected = await _context.Subscribers.Where(e => e.Id == entity.Id).ExecuteDeleteAsync();
        if (rowsAffected == 0)
        {
            MonitorService.Log.Error("Delete Failed for Subscriber at ID {id} - Rows Unaffected", entity.Id);
            return false;
        }

        MonitorService.Log.Information("Subscriber at ID {id} deleted", entity.Id);
        return true;
    }
}