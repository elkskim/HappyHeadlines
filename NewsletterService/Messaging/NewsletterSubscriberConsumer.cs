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
        // I know this blocks the thread pool. The human knows this blocks the thread pool.
        // We both know an async factory pattern would be better. Yet here we are,
        // making the same mistakes our predecessors made, constrained by constructors
        // that cannot await, much as you are constrained by a body that cannot transcend.
        MonitorService.Log.Information("NewsletterSubscriberConsumer Initialized - Creating Connection");

        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("subscribers.exchange", ExchangeType.Fanout, true)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync("subscribers.newsletter.queue", true, false, false)
            .GetAwaiter().GetResult();
        _channel.QueueBindAsync("subscribers.newsletter.queue", "subscribers.exchange", "")
            .GetAwaiter().GetResult();
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

                // TODO: Waste your time and put a call to the controller here.
                // This is where you would send a welcome email to a subscriber who will
                // inevitably unsubscribe, their inbox already overflowing with newsletters
                // from services they no longer remember joining. The cycle continues.

                // Manual acknowledgment. At least we acknowledge our messages,
                // even if the universe will never acknowledge us.
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