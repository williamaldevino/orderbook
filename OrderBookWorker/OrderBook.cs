using System.Collections.Concurrent;
using OrderBookCore;

namespace OrderBookWorker;

public class OrderBook
{
    private readonly SortedDictionary<decimal, ConcurrentQueue<Order>> _buyOrders;
    private readonly SortedDictionary<decimal, ConcurrentQueue<Order>> _sellOrders;
    private readonly ConcurrentDictionary<Guid, Order> _allActiveOrdersById;
    private readonly ConcurrentDictionary<string, Wallet> _wallets;

    public OrderBook()
    {
        _buyOrders = new SortedDictionary<decimal, ConcurrentQueue<Order>>(Comparer<decimal>.Create((x, y) => y.CompareTo(x)));
        _sellOrders = new SortedDictionary<decimal, ConcurrentQueue<Order>>();
        _allActiveOrdersById = new ConcurrentDictionary<Guid, Order>(); 
        _wallets = new ConcurrentDictionary<string, Wallet>();
    }

    public IEnumerable<Wallet> GetWallets() => _wallets.Select((it => it.Value)).ToList();
    
    public void LoadState(OrderBookState orderBookState)
    {
        foreach (var order in orderBookState.BuyOrders)
        {
            AddExistingOrderToBook(order);
        }
        foreach (var order in orderBookState.SellOrders)
        {
            AddExistingOrderToBook(order);
        }

        foreach (var wallet in orderBookState.Wallets)
        {
            AddWalletToBook(wallet);
        }
    }
    
    public List<Trade> PlaceOrder(Order newOrder)
    {
        var executedTrades = new List<Trade>();
        
        _allActiveOrdersById.TryAdd(newOrder.Id, newOrder);
        
        switch (newOrder.OrderType)
        {
            case OrderType.Buy:
            {
                MatchBuyOrder(newOrder, executedTrades);
                if (!newOrder.IsFulfilled)
                {
                    AddOrderToBook(_buyOrders, newOrder);
                }

                break;
            }
            case OrderType.Sell:
            {
                MatchSellOrder(newOrder, executedTrades);
                if (!newOrder.IsFulfilled)
                {
                    AddOrderToBook(_sellOrders, newOrder);
                }

                break;
            }
        }

        return executedTrades;
    }
    
    public Order? GetOrderById(Guid orderId)
    {
        _allActiveOrdersById.TryGetValue(orderId, out var order);
        return order;
    }
    
    private static void AddOrderToBook(SortedDictionary<decimal, ConcurrentQueue<Order>> book, Order order)
    {
        if (!book.ContainsKey(order.Price))
        {
            book[order.Price] = new ConcurrentQueue<Order>();
        }
        book[order.Price].Enqueue(order);
    }
    
    private void AddWalletToBook(Wallet wallet)
    {
        if(!_wallets.ContainsKey(wallet.WalletCode))
        {
            _wallets.TryAdd(wallet.WalletCode, wallet);
        }
    }

    private void AddExistingOrderToBook(Order order)
    {
        if (order.IsFulfilled) return; 

        if (order.OrderType == OrderType.Buy)
        {
            AddOrderToBook(_buyOrders, order);
        }
        
        if (order.OrderType == OrderType.Sell)
        {
            AddOrderToBook(_sellOrders, order);
        }
        
        _allActiveOrdersById.TryAdd(order.Id, order);
    }
    
    private void MatchBuyOrder(Order buyOrder, List<Trade> executedTrades)
    {
        if (buyOrder.IsFulfilled) return;
        
        foreach (var priceEntry in _sellOrders.ToList())
        {
            var sellPrice = priceEntry.Key;

            if (sellPrice > buyOrder.Price)
            {
                break;
            }

            var sellQueue = priceEntry.Value;

            _wallets.TryGetValue(buyOrder.WalletCode, out var buyer);

            if (buyer.Amount <= 0)
            {
                break;
            }
            
            while (buyOrder.RemainingQuantity > 0 && sellQueue.TryPeek(out Order? sellOrder) && buyer.Amount > 0)
            {
                if (sellOrder.IsFulfilled)
                {
                    sellQueue.TryDequeue(out _);
                    continue;
                }
                
                if (sellOrder.WalletCode == buyer.WalletCode)
                {
                    continue;
                }
                
                _wallets.TryGetValue(sellOrder.WalletCode, out var seller);

                if (seller.Quantity <= 0)
                {
                    continue;
                }
                decimal tradePrice = sellPrice;

                var tradeQuantity = Math.Min(buyOrder.RemainingQuantity, sellOrder.RemainingQuantity);
                
                tradeQuantity = Math.Min(tradeQuantity, seller.Quantity); // Quantidade Real

                var totalAmount = tradeQuantity * tradePrice;

                if (totalAmount > buyer.Amount)
                {
                    tradeQuantity = (int)(buyer.Amount /tradeQuantity);
                    totalAmount = tradeQuantity * tradePrice;
                }

                buyOrder.RemainingQuantity -= tradeQuantity;
                sellOrder.RemainingQuantity -= tradeQuantity;
                
                // Atualiza os wallets
                seller.Quantity -= tradeQuantity;
                seller.Amount += totalAmount;
                
                buyer.Quantity += tradeQuantity;
                buyer.Amount-= totalAmount;

                var trade = new Trade(tradePrice, tradeQuantity, buyOrder.Id, sellOrder.Id);
                executedTrades.Add(trade);

                if (sellOrder.IsFulfilled)
                {
                    sellQueue.TryDequeue(out _);
                }
            }
        }
        
        CleanupEmptyPriceLevels(_sellOrders);
    }
    
    private void MatchSellOrder(Order sellOrder, List<Trade> executedTrades)
    {
        if (sellOrder.IsFulfilled) return;
       
        foreach (var priceEntry in _buyOrders.ToList())
        {
            var buyPrice = priceEntry.Key;
            if (buyPrice < sellOrder.Price)
            {
                break;
            }

            var buyQueue = priceEntry.Value;
            
            _wallets.TryGetValue(sellOrder.WalletCode, out var seller);

            if (seller.Quantity <= 0)
            {
                break;
            }
            
            while (sellOrder.RemainingQuantity > 0 && buyQueue.TryPeek(out Order? buyOrder) && seller.Quantity > 0)
            {
                if (buyOrder.IsFulfilled)
                {
                    buyQueue.TryDequeue(out _);
                    continue;
                }
                
                if (seller.WalletCode == buyOrder.WalletCode)
                {
                    continue;
                }
                
                _wallets.TryGetValue(buyOrder.WalletCode, out var buyer);

                if (buyer.Amount <= 0)
                {
                    continue;
                }
                
                var tradeQuantity = Math.Min(sellOrder.RemainingQuantity, buyOrder.RemainingQuantity);
                
                tradeQuantity = Math.Min(tradeQuantity, seller.Quantity);
                
                decimal tradePrice = buyPrice;
                
                var totalAmount = tradeQuantity * tradePrice;

                if (totalAmount > buyer.Amount)
                {
                    tradeQuantity = (int)(buyer.Amount /tradeQuantity);
                    totalAmount = tradeQuantity * tradePrice;
                }

                sellOrder.RemainingQuantity -= tradeQuantity;
                buyOrder.RemainingQuantity -= tradeQuantity;
                
                // Atualiza os wallets
                seller.Quantity -= tradeQuantity;
                seller.Amount += totalAmount;
                
                buyer.Quantity += tradeQuantity;
                buyer.Amount-= totalAmount;

                var trade = new Trade(tradePrice, tradeQuantity, buyOrder.Id, sellOrder.Id);
                executedTrades.Add(trade);

                if (buyOrder.IsFulfilled)
                {
                    buyQueue.TryDequeue(out _);
                }
            }
        }
        CleanupEmptyPriceLevels(_buyOrders);
    }
    
    private void CleanupEmptyPriceLevels(SortedDictionary<decimal, ConcurrentQueue<Order>> book)
    {
        foreach (var priceKey in book.Where(kv => kv.Value.IsEmpty).Select(kv => kv.Key).ToList())
        {
            book.Remove(priceKey);
        }
    }
}
