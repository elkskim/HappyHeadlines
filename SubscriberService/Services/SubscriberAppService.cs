using Microsoft.Extensions.Logging.Abstractions;
using Monitoring;
using SubscriberDatabase.Data;
using SubscriberDatabase.Model;
using SubscriberService.Messaging;
using SubscriberService.Models.DTO;
using SubscriberService.Models.Events;
using SubscriberService.Models.Mappers;

namespace SubscriberService.Services;

public class SubscriberAppService : ISubscriberAppService
{
    private readonly ISubscriberRepository _subscriberRepository;
    private readonly ISubscriberPublisher _subscriberPublisher;

    public SubscriberAppService(ISubscriberRepository subscriberRepository,  ISubscriberPublisher subscriberPublisher)
    {
        _subscriberRepository = subscriberRepository;
        _subscriberPublisher = subscriberPublisher;
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

        // We persist the subscriber to the database, then broadcast their arrival to a queue
        // that may or may not have listeners. This is faith: sending messages into the void,
        // hoping someone, somewhere, is listening. Most of the time, no one is.
        var evt = new SubscriberAddedEvent
        {
            Id = subscriber.Id,
            Email = subscriber.Email,
            Region = subscriber.Region,
            SubscribedOn = subscriber.SubscribedOn
        };
        
        await _subscriberPublisher.PublishSubscriberAdded(evt);
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

        var evt = new SubscriberUpdatedEvent
        {
            Email = updatedEntity.Email,
            Region = updatedEntity.Region
        };
        
        await _subscriberPublisher.PublishSubscriberUpdated(evt);
        
        MonitorService.Log.Information("Subscriber with ID {id} updated", id);
        return updatedEntity.ToReadDto();
    }

    public async Task<bool> Delete(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("SubscriberService.Delete");
        
        var entity = await _subscriberRepository.GetByIdAsync(id);
        if (entity == null)
        {
            MonitorService.Log.Warning("Subscriber with ID {id} was not found", id);
            return false;
        }

        var evt = new SubscriberRemovedEvent
        {
            Id = entity.Id,
            Email = entity.Email,
            Region = entity.Region
        };
        
        var deleted =  await _subscriberRepository.DeleteAsync(id);
        
        if (!deleted)
        {
            MonitorService.Log.Warning("Subscriber with ID {id} failed to delete", id);
            return false;
        }
        
        MonitorService.Log.Information("Subscriber at ID {id} deleted", id);
        await _subscriberPublisher.PublishSubscriberRemoved(evt);
        return true;
        
    }
}