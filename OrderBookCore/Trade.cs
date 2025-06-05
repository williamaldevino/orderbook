namespace OrderBookCore;

public record Trade(decimal Price,int Quantity, Guid BuyOrderId, Guid SellOrderId)
{
    public Guid Id { get; init; }  = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}