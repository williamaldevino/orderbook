namespace OrderBookCore;

public class OrderBookState
{
    public IEnumerable<Order> BuyOrders { get; init; } = new List<Order>();
    public IEnumerable<Order> SellOrders { get; init; } = new List<Order>();
    
    public IEnumerable<Wallet> Wallets { get; init; } = new List<Wallet>();
}