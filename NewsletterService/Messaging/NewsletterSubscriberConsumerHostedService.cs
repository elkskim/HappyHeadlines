using Monitoring;
using NewsletterService.Features;

namespace NewsletterService.Messaging;

public class NewsletterSubscriberConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly NewsletterSubscriberConsumer _consumer;
    private readonly IFeatureToggleService _features;

    public NewsletterSubscriberConsumerHostedService(NewsletterSubscriberConsumer consumer, IFeatureToggleService features)
    {
        _consumer = consumer;
        _features = features;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_features.IsSubscriberServiceEnabled())
        {
            MonitorService.Log.Warning("SubscriberService disabled - SubscriberConsumer not started.");
            return Task.CompletedTask;
        }

        MonitorService.Log.Information("Starting NewsletterSubscriberConsumer...");
        _consumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Stopping NewsletterSubscriberConsumer...");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _consumer.DisposeAsync();
    }
}
