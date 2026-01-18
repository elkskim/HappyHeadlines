using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ArticleDatabase.Models;
using Monitoring;
using RabbitMQ.Client;

namespace PublisherService.Services;

public class PublisherMessaging
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;

    public PublisherMessaging(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public async static Task<PublisherMessaging> CreateAsync()
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        
        int attempt = 0;
        int maxAttempts = 10;
        int delayMs = 2000;
        
        while (attempt < maxAttempts)
        {
            try
            {
                MonitorService.Log.Information("Connecting to RabbitMQ (attempt {Attempt}/{Max})", attempt + 1, maxAttempts);
                var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true).GetAwaiter().GetResult();
                
                MonitorService.Log.Information("Successfully connected to RabbitMQ and declared exchange");
                return new PublisherMessaging(connection, channel);
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxAttempts)
                {
                    MonitorService.Log.Error(ex, "Failed to connect to RabbitMQ after {Attempts} attempts", maxAttempts);
                    throw;
                }
                
                MonitorService.Log.Warning(ex, "RabbitMQ connection failed (attempt {Attempt}/{Max}); retrying in {Delay}ms", attempt, maxAttempts, delayMs);
                Thread.Sleep(delayMs);
                delayMs = Math.Min(delayMs * 2, 30000);
            }
        }
        
        throw new InvalidOperationException("Failed to connect to RabbitMQ; retry loop exhausted without success");
    }

    public async Task<Article> PublishArticle(Article article)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("PublishArticle");
        MonitorService.Log.Information("Publishing article with TraceId: {TraceId}", activity?.TraceId);
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(article));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>()
        };
        
        // CRITICAL: Inject trace context into message headers for distributed tracing
        // This ensures traces are NOT broken when crossing service boundaries
        if (activity != null)
        {
            properties.Headers["traceparent"] = $"00-{activity.TraceId}-{activity.SpanId}-01";
            properties.Headers["tracestate"] = activity.TraceStateString ?? string.Empty;
            MonitorService.Log.Information("Injected trace context: traceparent={TraceParent}", properties.Headers["traceparent"]);
        }
        
        if (_channel == null) throw new ArgumentNullException(nameof(_channel));

        await _channel.BasicPublishAsync(
            "articles.exchange",
            "",
            false,
            properties,
            body
        );

        return article;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}