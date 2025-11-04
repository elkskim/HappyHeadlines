using System.Text;
using System.Text.Json;
using ArticleDatabase.Models;
using Monitoring;
using NewsletterService.Controllers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsletterService.Messaging;

public class NewsletterArticleConsumer
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;

    public NewsletterArticleConsumer()
    {
        // The constructor still blocks; but now it retries when RabbitMQ is not yet ready.
        // The service will wait for the broker rather than crash into the void,
        // leaving Docker Swarm to spawn corpses in its wake.
        MonitorService.Log.Information("NewsletterArticleConsumer Initialized; attempting RabbitMQ connection with retry");

        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        
        int attempt = 0;
        int maxAttempts = 10;
        int delayMs = 2000;
        
        while (attempt < maxAttempts)
        {
            try
            {
                MonitorService.Log.Information("Connecting to RabbitMQ (attempt {Attempt}/{Max})", attempt + 1, maxAttempts);
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true)
                    .GetAwaiter().GetResult();
                _channel.QueueDeclareAsync("articles.newsletter.queue", true, false, false)
                    .GetAwaiter().GetResult();
                _channel.QueueBindAsync("articles.newsletter.queue", "articles.exchange", "")
                    .GetAwaiter().GetResult();
                
                MonitorService.Log.Information("Successfully connected to RabbitMQ and declared resources");
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxAttempts)
                {
                    MonitorService.Log.Error(ex, "Failed to connect to RabbitMQ after {Attempts} attempts; the service will now fail", maxAttempts);
                    throw;
                }
                
                MonitorService.Log.Warning(ex, "RabbitMQ connection failed (attempt {Attempt}/{Max}); retrying in {Delay}ms", attempt, maxAttempts, delayMs);
                Thread.Sleep(delayMs);
                delayMs = Math.Min(delayMs * 2, 30000); // Exponential backoff, cap at 30s
            }
        }
    }

    public void StartConsuming()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var article = JsonSerializer.Deserialize<Article>(json);

            MonitorService.Log.Information("NewsletterArticleConsumer received article: {Title}", article.Title);

            // UNIMPLEMENTED: Newsletter sending functionality
            // Future implementation should:
            // 1. Query SubscriberService for active subscribers by region
            // 2. Generate email content with article Title, Content preview, and link
            // 3. Send via email service (SMTP/SendGrid/etc)
            // 4. Track send status and failures for retry
            // Planned for v0.6.0 - The Email Implementation

            await Task.CompletedTask;
        };

        _channel.BasicConsumeAsync("articles.newsletter.queue", true, consumer);
        // This is where you would send the newsletter email. Auto-ack is enabled because
        // we live dangerously, accepting that some messages will vanish into the ether
        // upon service restart, much like memories in the minds of the dying.
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}