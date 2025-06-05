namespace OrderBookCore;

public record Order(
    Guid Id,
    String WalletCode,
    OrderType OrderType,
    decimal Price,
    int Quantity)
{
    public int RemainingQuantity { get; set; } = Quantity;
    
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public bool IsFulfilled => RemainingQuantity <= 0;
    public static Order NewAsk(string wallet,decimal price, int quantity)
    {
        var orderId = Guid.NewGuid();
        return new Order(orderId,wallet, OrderType.Sell, price, quantity);
    }
     
    public static Order NewBid(string wallet, decimal price, int quantity)
    {
        var orderId = Guid.NewGuid();
        return new Order(orderId,wallet, OrderType.Buy, price, quantity);
    }
}