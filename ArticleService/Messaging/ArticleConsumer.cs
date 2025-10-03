using System.Text;
using System.Text.Json;
using ArticleDatabase.Models;
using Monitoring;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArticleService.Messaging;

public class ArticleConsumer
{
    private readonly ArticleDbContext _articleDbContext;
    private readonly IChannel _channel;
    private readonly IConnection _connection;

    public ArticleConsumer()
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync("articles.queue", true, false, false)
            .GetAwaiter().GetResult();
        _channel.QueueBindAsync("article.persist.queue", "articles.exchange", "")
            .GetAwaiter().GetResult();
    }

    public void Consume()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            using var activity = MonitorService.ActivitySource.StartActivity("ConsumeArticle");
            
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var article = JsonSerializer.Deserialize<Article>(json);

            MonitorService.Log.Information("ArticleService Received Article: {article?.Title}", article?.Title);

            await _articleDbContext.Articles.AddAsync(article);
            await _articleDbContext.SaveChangesAsync();
            await Task.CompletedTask;
        };

        _channel.BasicConsumeAsync("article.persist.queue", true, consumer);
    }
}