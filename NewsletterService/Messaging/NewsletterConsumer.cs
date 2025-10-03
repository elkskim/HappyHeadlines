using System.Text;
using System.Text.Json;
using ArticleDatabase.Models;
using Monitoring;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsletterService.Messaging;

public class NewsletterConsumer
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;

    public NewsletterConsumer()
    {
        MonitorService.Log.Information("NewsletterConsumer Initialized - Creating Connection");

        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync("article.newsletter.queue", true, false, false)
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

            MonitorService.Log.Information("NewsletterConsumer received article: {Title}", article.Title);

            //TODO waste your time and put a call to the controller here

            await Task.CompletedTask;
        };

        _channel.BasicConsumeAsync("article.newsletter.queue", true, consumer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}