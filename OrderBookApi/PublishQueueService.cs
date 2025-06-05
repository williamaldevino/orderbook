using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderBookCore;
using RabbitMQ.Client;

namespace OrderBookApi;

public class PublishQueueService: IDisposable
{
    private readonly ILogger<PublishQueueService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;

    public PublishQueueService(
        IOptions<RabbitMQOptions> options,
        ILogger<PublishQueueService> logger)
    {
        _logger = logger;
        _queueName = options.Value.OrderQueueName;
        
        var factory = new ConnectionFactory()
        {
            HostName = options.Value.HostName, 
            DispatchConsumersAsync = true,
            UserName = options.Value.UserName,
            Password = options.Value.Password
        }; 
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }
    
    public void PublishMessage<T>(string correlationId, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.CorrelationId = correlationId;

        _channel.BasicPublish(exchange: string.Empty, // Exchange padrão
            routingKey: _queueName,
            basicProperties: properties,
            body: body);
        
        _logger.LogInformation($"Sent '{json}' to queue '{_queueName}'");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _connection?.Dispose();
        _channel?.Dispose();
    }
}
