using OrderBookCore;

namespace OrderBookApi;

public class OrderBookService(OrderRepository orderRepository, PublishQueueService publishQueueService)
{
    public Task<(string? errorMessage,Order?)> PlaceAskAsync(string correlationId, PlaceOrder placedOrder) 
        => PlaceOrderAsync(correlationId,placedOrder,Order.NewAsk);
    
    
    public Task<(string? errorMessage,Order?)> PlaceBidAsync(string correlationId, PlaceOrder placedOrder) => 
        PlaceOrderAsync(correlationId,placedOrder,Order.NewBid);
    
    private async Task<(string? errorMessage,Order?)> PlaceOrderAsync(string correlationId, PlaceOrder placedOrder, Func<string,decimal,int,Order> factoryOrder )
    {
        var wallet = await orderRepository.GetWalletByCodeAsync(placedOrder.WalletCode);

        if (wallet == null)
        {
            return ("Wallet not found",null);
        }
        
        var order = factoryOrder(placedOrder.WalletCode, placedOrder.Price,placedOrder.Quantity);
    
        publishQueueService.PublishMessage(correlationId, order);
        
        return (null,order);
    }
}