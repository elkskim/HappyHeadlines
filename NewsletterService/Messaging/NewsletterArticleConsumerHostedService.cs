using Monitoring;
using NewsletterService.Messaging;

public class NewsletterArticleConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly NewsletterArticleConsumer _articleConsumer;

    public NewsletterArticleConsumerHostedService(NewsletterArticleConsumer articleConsumer)
    {
        _articleConsumer = articleConsumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Starting NewsletterArticleConsumerHostedService...");
        _articleConsumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log.Information("Stopping NewsletterArticleConsumerHostedService...");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _articleConsumer.DisposeAsync();
    }
}