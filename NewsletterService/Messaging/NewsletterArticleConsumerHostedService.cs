using Monitoring;
using NewsletterService.Messaging;

public class NewsletterConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly NewsletterConsumer _consumer;

    public NewsletterConsumerHostedService(NewsletterConsumer consumer)
    {
        _consumer = consumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Starting NewsletterConsumerHostedService...");
        _consumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Stopping NewsletterConsumerHostedService...");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _consumer.DisposeAsync();
    }
}