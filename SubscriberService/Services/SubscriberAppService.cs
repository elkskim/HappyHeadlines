using Microsoft.Extensions.Logging.Abstractions;
using Monitoring;
using SubscriberDatabase.Data;
using SubscriberDatabase.Model;
using SubscriberService.Models.DTO;
using SubscriberService.Models.Mappers;

namespace SubscriberService.Services;

public class SubscriberAppService : ISubscriberAppService
{
    private readonly ISubscriberRepository _subscriberRepository;

    public SubscriberAppService(ISubscriberRepository subscriberRepository)
    {
        _subscriberRepository = subscriberRepository;
    }
    
    public async Task<IEnumerable<SubscriberReadDto>?> GetSubscribers()
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.GetSubscribers"); 
        var subscribers = await _subscriberRepository.GetSubscribersAsync();
        return subscribers.Select(s => s.ToReadDto());
    }

    public async Task<SubscriberReadDto?> GetById(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.GetById"); 
        var subscriber = await _subscriberRepository.GetByIdAsync(id);

        if (subscriber != null) return subscriber.ToReadDto();
        MonitorService.Log.Warning("Subscriber with ID {id} was not found", id);
        return null;
    }

    public async Task<SubscriberReadDto?> Create(SubscriberCreateDto entity)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.Create"); 
        
        var subscriber = await _subscriberRepository.CreateAsync(entity.ToEntity());
        return subscriber.ToReadDto();
    }

    public async Task<SubscriberReadDto?> Update(int id, SubscriberUpdateDto newEntity)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.Update"); 
        
        var oldEntity = await _subscriberRepository.GetByIdAsync(id);
        if (oldEntity == null)
        {
            MonitorService.Log.Warning("Subscriber with ID {id} was not found", id);
            return null;
        }
        
        // Remember the mapper... reeeemembeeeerrr...
        oldEntity.ApplyUpdate(newEntity);
        
        var updatedEntity = await _subscriberRepository.UpdateAsync(id, oldEntity);
        if (updatedEntity == null)
        {
            MonitorService.Log.Warning("Subscriber with ID {id} failed to update", id);
            return null;
        }
        
        MonitorService.Log.Information("Subscriber with ID {id} updated", id);
        return updatedEntity.ToReadDto();
    }

    public async Task<bool> Delete(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.Delete");
        return await  _subscriberRepository.DeleteAsync(id);
    }
}