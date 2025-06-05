using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // For logging
using OrderBookCore;

namespace OrderBookWorker
{
    public class OrderBookService : BackgroundService
    {
        private readonly ILogger<OrderBookService> _logger;
        private readonly OrderRepository _orderRepository;
        private readonly ConsumerQueueService _queueService;
        private OrderBook? _orderBook; 

        public OrderBookService(
            ILogger<OrderBookService> logger, 
            ConsumerQueueService queueService,
            OrderRepository orderRepository)
        {
            _logger = logger;
            _queueService = queueService;
            _orderRepository = orderRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderBook running.");

            try
            {
                _queueService!.StartConsuming(async (messageJson) =>
                {
                    _orderBook = new OrderBook(); // Initialize OrderBook here, or inject it if it needs to be a singleton

                    await LoadInitialOrderBookState();

                    var order = JsonSerializer.Deserialize<Order>(messageJson);

                    if (order == null)
                    {
                        _logger.LogWarning("Invalid order message received.");
                        return;
                    }

                    await _orderRepository!.SaveOrderAsync(order);

                    var executedTrades = _orderBook.PlaceOrder(order);

                    foreach (var trade in executedTrades)
                    {
                        await _orderRepository.SaveTradeAsync(trade);
                    }

                    var affectedOrders = new List<Order> { order };

                    foreach (var trade in executedTrades)
                    {
                        var buyOrder = _orderBook.GetOrderById(trade.BuyOrderId);

                        if (buyOrder != null && !affectedOrders.Contains(buyOrder))
                            affectedOrders.Add(buyOrder);

                        var sellOrder = _orderBook.GetOrderById(trade.SellOrderId);
                        if (sellOrder != null && !affectedOrders.Contains(sellOrder))
                            affectedOrders.Add(sellOrder);
                    }

                    foreach (var orderToUpdate in affectedOrders)
                    {
                        await _orderRepository.UpdateOrderQuantityAsync(orderToUpdate.Id, orderToUpdate.RemainingQuantity);
                    }

                    foreach (var wallet in _orderBook.GetWallets())
                    {
                        await _orderRepository.UpdateWalletAsync(wallet.WalletCode, wallet.Amount,wallet.Quantity);
                        
                        _logger.LogInformation($"Updated wallet {wallet.WalletCode}: Amount {wallet.Amount} Quantity {wallet.Quantity}");
                    }

                    if (executedTrades.Any())
                    {
                        _logger.LogInformation($"Executed {executedTrades.Count} trades:");
                        foreach (var trade in executedTrades)
                        {
                            _logger.LogInformation($"- Trade: {trade.Quantity} @ {trade.Price}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No trades executed for this order.");
                    }
                });
                
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("OrderBook is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in OrderBook.");
            }
            finally
            {
                _queueService?.Dispose();
                _logger.LogInformation("OrderBook stopped.");
            }
        }

        private async Task LoadInitialOrderBookState()
        {
            var orderBookState = await _orderRepository!.LoadOrderBookStateAsync();
            
            _orderBook.LoadState(orderBookState);
        }

       
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OrderBook is gracefully shutting down.");
            _queueService?.Dispose(); // Ensure disposal on stop
            await base.StopAsync(cancellationToken);
        }
    }
}