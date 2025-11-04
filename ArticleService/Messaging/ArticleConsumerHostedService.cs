using Monitoring;

namespace ArticleService.Messaging;

public class ArticleConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly ArticleConsumer _articleConsumer;

    public ArticleConsumerHostedService(ArticleConsumer articleConsumer)
    {
        Console.WriteLine("=== ARTICLECONSUMERHOSTEDSERVICE CONSTRUCTOR CALLED ===");
        MonitorService.Log?.Information("ArticleConsumerHostedService is being instantiated");
        _articleConsumer = articleConsumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("=== ARTICLECONSUMERHOSTEDSERVICE STARTASYNC CALLED ===");
        MonitorService.Log?.Information("Starting ArticleConsumerHostedService; initiating consumer...");
        _articleConsumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        MonitorService.Log?.Information("Stopping ArticleConsumerHostedService...");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _articleConsumer.DisposeAsync();
    }
}