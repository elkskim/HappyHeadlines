// New interface for publisher. Allows mocking in tests and keeps DI clean.
using SubscriberService.Models.Events;

namespace SubscriberService.Messaging;

public interface ISubscriberPublisher : IAsyncDisposable
{
    Task PublishSubscriberAdded(SubscriberAddedEvent evt);
    Task PublishSubscriberUpdated(SubscriberUpdatedEvent evt);
    Task PublishSubscriberRemoved(SubscriberRemovedEvent evt);
}

