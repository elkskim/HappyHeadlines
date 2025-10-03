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

    public PublisherMessaging()
    {
        using var activity = MonitorService.ActivitySource.StartActivity("PublishArticle");
        MonitorService.Log.Information("Publishing Article");
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true).GetAwaiter().GetResult();
    }

    public async Task<Article> PublishArticle(Article article)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(article));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

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