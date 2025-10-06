using ArticleService.Messaging;
using Monitoring;

public class ArticleConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly ArticleConsumer _consumer;

    public ArticleConsumerHostedService(ArticleConsumer consumer)
    {
        _consumer = consumer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Starting ArticleConsumerHostedService...");
        await _consumer.Consume();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Stopping ArticleConsumerHostedService...");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _consumer.DisposeAsync();
    }
}