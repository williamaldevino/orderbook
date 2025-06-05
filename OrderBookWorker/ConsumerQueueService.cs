using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderBookCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace OrderBookWorker;

public class ConsumerQueueService: IDisposable
{
    private readonly ILogger<ConsumerQueueService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    
    public ConsumerQueueService(
        IOptions<RabbitMQOptions> rabbitMqOptions, 
        ILogger<ConsumerQueueService> logger)
    {
        _logger = logger;
        
        var options = rabbitMqOptions.Value;
        _queueName = options.OrderQueueName;
        
        var factory = new ConnectionFactory()
        {
            HostName = options.HostName, 
            DispatchConsumersAsync = true,
            UserName = options.UserName,
            Password = options.Password
        }; 
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }
    
    public void StartConsuming(Func<string, Task> onMessageReceived)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            
            var body = ea.Body.ToArray();
            var correlationId = ea.BasicProperties.CorrelationId;

            using (LogContext.PushProperty("CorrelationId", correlationId ?? "none")) ;
            
            var message = Encoding.UTF8.GetString(body);
            
            _logger.LogInformation($"Received '{message}' with CorrelationId '{correlationId} from queue '{_queueName}'");

            try
            {
                await onMessageReceived(message);
                _channel.BasicAck(ea.DeliveryTag, multiple: false); 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message} with CorrelationId '{correlationId}");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true); 
            }
        };

        _channel.BasicConsume(queue: _queueName,
            autoAck: false,
            consumer: consumer);
        _logger.LogError($" [*] Waiting for messages in queue '{_queueName}'. To exit press CTRL+C.");
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _channel?.Dispose();
        _connection?.Dispose();
    }
}