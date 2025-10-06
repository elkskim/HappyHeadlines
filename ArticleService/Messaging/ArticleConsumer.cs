using System.Text;
using System.Text.Json;
using ArticleDatabase.Models;
using Monitoring;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArticleService.Messaging;

public class ArticleConsumer : IAsyncDisposable
{
    private readonly ArticleDbContext _articleDbContext;
    private IChannel? _channel;
    private IConnection? _connection;

    public ArticleConsumer(ArticleDbContext articleDbContext)
    {
        _articleDbContext = articleDbContext;
        
    }

    public async Task Consume()
    {
        MonitorService.Log.Information("It is time. The ArticleConsumer Rises to Devour.");
        //God grant me strength
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
       _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync("articles.exchange", ExchangeType.Fanout, true)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync("articles.persist.queue", true, false, false)
            .GetAwaiter().GetResult();
        _channel.QueueBindAsync("articles.persist.queue", "articles.exchange", "")
            .GetAwaiter().GetResult();
        
        
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            using var activity = MonitorService.ActivitySource.StartActivity("ConsumeArticle");
            
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var article = JsonSerializer.Deserialize<Article>(json);

            MonitorService.Log.Information("ArticleService Received Article: {article?.Title}", article?.Title);

            //congratulatuiiasns, here is the article. Let us hope it's not null
            await _articleDbContext.Articles.AddAsync(article);
            await _articleDbContext.SaveChangesAsync();
            await Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync("articles.persist.queue", true, consumer);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}