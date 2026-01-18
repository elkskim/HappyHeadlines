using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ArticleDatabase;
using ArticleDatabase.Models;
using Monitoring;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArticleService.Messaging;

public class ArticleConsumer : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private IChannel? _channel;
    private IConnection? _connection;

    public ArticleConsumer(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== ARTICLECONSUMER CONSTRUCTOR CALLED ===");
        // Don't log in constructor; logger may not be initialized yet
        _serviceProvider = serviceProvider;
    }

    public void StartConsuming()
    {
        Console.WriteLine("=== STARTCONSUMING CALLED ===");
        MonitorService.Log?.Information("StartConsuming called; spawning background task for RabbitMQ connection");
        // Initialize connection in background thread to not block startup
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("=== TASK.RUN STARTED - CONNECTING TO RABBITMQ ===");
                MonitorService.Log?.Information("It is time. The ArticleConsumer Rises to Devour; but first, it must wait for the broker.");
                
                var factory = new ConnectionFactory { HostName = "rabbitmq" };
                
                int maxAttempts = 10;
                int delayMs = 2000;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        MonitorService.Log?.Information("Connecting to RabbitMQ (attempt {Attempt}/{Max})", attempt + 1, maxAttempts);
                        _connection = await factory.CreateConnectionAsync();
                        _channel = await _connection.CreateChannelAsync();
                        await _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true);
                        await _channel.QueueDeclareAsync("articles.persist.queue", true, false, false);
                        await _channel.QueueBindAsync("articles.persist.queue", "articles.exchange", "");
                        
                        MonitorService.Log?.Information("Successfully connected to RabbitMQ and declared resources");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt >= maxAttempts - 1)
                        {
                            MonitorService.Log?.Error(ex, "Failed to connect to RabbitMQ after {Attempts} attempts; the consumer cannot function", maxAttempts);
                            return;
                        }
                        
                        MonitorService.Log?.Warning(ex, "RabbitMQ connection failed (attempt {Attempt}/{Max}); retrying in {Delay}ms", attempt + 1, maxAttempts, delayMs);
                        await Task.Delay(delayMs);
                        delayMs = Math.Min(delayMs * 2, 30000);
                    }
                }
                
                if (_channel == null)
                {
                    MonitorService.Log?.Error("Cannot start consuming; channel is null after initialization");
                    return;
                }
                
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        // CRITICAL: Extract trace context from message headers to continue distributed trace
                        // This ensures traces are NOT broken when crossing service boundaries
                        ActivityContext parentContext = default;
                        
                        if (ea.BasicProperties.Headers != null && 
                            ea.BasicProperties.Headers.TryGetValue("traceparent", out var traceparentObj) &&
                            traceparentObj is byte[] traceparentBytes)
                        {
                            var traceparent = Encoding.UTF8.GetString(traceparentBytes);
                            MonitorService.Log?.Information("Extracted traceparent from message: {TraceParent}", traceparent);
                            
                            // Parse W3C traceparent format: "00-{traceId}-{spanId}-{flags}"
                            var parts = traceparent.Split('-');
                            if (parts.Length == 4)
                            {
                                var traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
                                var spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
                                parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded, isRemote: true);
                            }
                        }
                        
                        // Start activity as a child of the publisher's span
                        using var activity = MonitorService.ActivitySource.StartActivity(
                            "ConsumeArticle",
                            ActivityKind.Consumer,
                            parentContext != default ? parentContext : default);
                        
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        Console.WriteLine($"=== ARTICLE RECEIVED === {json}");
                        MonitorService.Log?.Information("ArticleConsumer received message with TraceId: {TraceId}. Payload: {Json}", 
                            activity?.TraceId, json);
                        
                        var article = JsonSerializer.Deserialize<Article>(json);

                        if (article == null)
                        {
                            MonitorService.Log?.Warning("Failed to deserialize article from message");
                            return;
                        }

                        MonitorService.Log?.Information("Deserialized article: {Title}, Author: {Author}, Region: {Region}", article.Title, article.Author, article.Region);

                        using var scope = _serviceProvider.CreateScope();
                        var dbContextFactory = scope.ServiceProvider.GetRequiredService<DbContextFactory>();
                        
                        // Use the article's region to determine which database to persist to
                        var dbContext = dbContextFactory.CreateDbContext(new[] { "region", article.Region ?? "Global" });
                        
                        // Child span for database operation - provides granular timing in traces
                        using var dbActivity = MonitorService.ActivitySource.StartActivity("SaveArticleToDatabase");
                        dbActivity?.SetTag("article.title", article.Title);
                        dbActivity?.SetTag("article.author", article.Author);
                        dbActivity?.SetTag("article.region", article.Region ?? "Global");
                        
                        MonitorService.Log?.Information("Adding article to {Region} database context", article.Region);
                        await dbContext.Articles.AddAsync(article);
                        
                        MonitorService.Log?.Information("Saving changes to {Region} database", article.Region);
                        await dbContext.SaveChangesAsync();
                        
                        dbActivity?.SetTag("article.id", article.Id);
                        MonitorService.Log?.Information("Successfully persisted article: {Title} with ID: {Id} to {Region} database", article.Title, article.Id, article.Region);
                    }
                    catch (Exception ex)
                    {
                        MonitorService.Log?.Error(ex, "CRITICAL ERROR in ArticleConsumer while processing message. Message will be lost due to auto-ack.");
                    }
                };

                await _channel.BasicConsumeAsync("articles.persist.queue", true, consumer);
                MonitorService.Log?.Information("ArticleConsumer is now listening for articles on queue: articles.persist.queue");
            }
            catch (Exception ex)
            {
                MonitorService.Log?.Error(ex, "Fatal error in ArticleConsumer initialization");
            }
        });
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}

