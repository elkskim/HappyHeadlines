using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Monitoring;
using SubscriberService.Models.Events;

namespace SubscriberService.Messaging;

public class SubscriberPublisher : IAsyncDisposable
{
    private readonly IChannel _channel;
    private const string ExchangeName = "subscribers.exchange";

    public SubscriberPublisher(IChannel channel)
    {
        _channel = channel;
        
        // Declare exchange during initialization
        _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, durable: true)
            .GetAwaiter().GetResult();
    }

    public async Task PublishSubscriberAdded(SubscriberAddedEvent evt)
    {
        using var activity = MonitorService.ActivitySource
            .StartActivity("SubscriberPublisher.PublishSubscriberAdded");

        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(ExchangeName, "", false, props, body);
        MonitorService.Log.Information("Published SubscriberAddedEvent for {Email}", evt.Email);
    }

    public async Task PublishSubscriberRemoved(SubscriberRemovedEvent evt)
    {
        using var activity = MonitorService.ActivitySource
            .StartActivity("SubscriberPublisher.PublishSubscriberRemoved");

        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(ExchangeName, "", false, props, body);
        MonitorService.Log.Information("Published SubscriberRemovedEvent for {Email}", evt.Email);
    }

    public async Task PublishSubscriberUpdated(SubscriberUpdatedEvent evt)
    {
        using var activity = MonitorService.ActivitySource
            .StartActivity("SubscriberPublisher.PublishSubscriberUpdated");
        
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };
        
        await _channel.BasicPublishAsync(ExchangeName, "", false, props, body);
        MonitorService.Log.Information("Published SubscriberUpdatedEvent for {Email}", evt.Email);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
    }
}
