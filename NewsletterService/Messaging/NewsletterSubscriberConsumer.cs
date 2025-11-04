using System.Text;
using System.Text.Json;
using Monitoring;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NewsletterService.Models.Events;

namespace NewsletterService.Messaging;

public class NewsletterSubscriberConsumer
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;

    public NewsletterSubscriberConsumer()
    {
        // The constructor still blocks; but it retries when RabbitMQ is not yet ready.
        // We repeat our mistakes with slightly more grace, waiting for the broker
        // rather than dying immediately when it is not there to receive us.
        MonitorService.Log.Information("NewsletterSubscriberConsumer Initialized; attempting RabbitMQ connection with retry");

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

                _channel.ExchangeDeclareAsync("subscribers.exchange", ExchangeType.Fanout, true)
                    .GetAwaiter().GetResult();
                _channel.QueueDeclareAsync("subscribers.newsletter.queue", true, false, false)
                    .GetAwaiter().GetResult();
                _channel.QueueBindAsync("subscribers.newsletter.queue", "subscribers.exchange", "")
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
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var subscriber = JsonSerializer.Deserialize<Subscriber>(json);

                if (subscriber == null)
                {
                    MonitorService.Log.Warning("Failed to deserialize subscriber message. NACKing.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                MonitorService.Log.Information("NewsletterSubscriberConsumer received Subscriber: {Email}",
                    subscriber.Email);

                // UNIMPLEMENTED: Welcome email sending
                // Future implementation should:
                // 1. Generate personalized welcome email with unsubscribe link
                // 2. Send via email service
                // 3. Log send status for monitoring
                // 4. Handle failures with retry or dead-letter queue
                // Planned for v0.6.0 - The Email Implementation

                // Manual message acknowledgment
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                MonitorService.Log.Error(ex, "NewsletterSubscriberConsumer failed to process message");
                // Manual negative acknowledgment. The Copilot tried to make me smarter,
                // but I am not smart. It autofilled that sentence, which is hilarious.
                // We reject this message without requeue, sending it to the void,
                // much like our own inevitable journey into the heat death of the universe.
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsumeAsync("subscribers.newsletter.queue", false, consumer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}