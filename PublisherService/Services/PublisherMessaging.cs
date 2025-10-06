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
        var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true).GetAwaiter().GetResult();
        
        return new PublisherMessaging(connection, channel);
    }

    public async Task<Article> PublishArticle(Article article)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("PublishArticle");
        MonitorService.Log.Information("Publishing article");
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(article));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };
        
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