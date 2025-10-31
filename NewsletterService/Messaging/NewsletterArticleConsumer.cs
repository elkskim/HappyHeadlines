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
        // Another constructor blocking on async I/O. We repeat the pattern because
        // changing it would require rethinking the entire initialization lifecycle,
        // and who has time for that when deadlines loom like the heat death of stars?
        MonitorService.Log.Information("NewsletterArticleConsumer Initialized - Creating Connection");

        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync("articles.newsletter.queue", true, false, false)
            .GetAwaiter().GetResult();
        _channel.QueueBindAsync("articles.newsletter.queue", "articles.exchange", "")
            .GetAwaiter().GetResult();
    }

    public void StartConsuming()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var article = JsonSerializer.Deserialize<Article>(json);

            MonitorService.Log.Information("NewsletterArticleConsumer received article: {Title}", article.Title);

            // TODO: Waste your time and put a call to the controller here.
            // Send this article to subscribers who will skim the headline, never read it,
            // and forget it existed within the span of a single scroll. Content creation
            // in the digital age: shouting into the void, hoping for engagement metrics.

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