namespace OrderBookCore;

public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";
    public required string HostName { get; init; } 
    public required string OrderQueueName { get; init; }

    public required string UserName { get; init;}
    public required string Password { get; init; }
}