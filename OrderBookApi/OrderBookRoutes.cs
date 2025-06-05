using OrderBookCore;

namespace OrderBookApi;

public static class OrderBookRoutes
{
    public static WebApplication UseOrderBookRoutes(this WebApplication app)
    {
        app.MapPost("orders/asks", async (HttpContext context,OrderBookService orderBookService, PlaceOrder placedOrder) =>
        {
            var correlationId = context.TraceIdentifier;
            
            var (errorMessage,order) = await orderBookService.PlaceAskAsync(correlationId, placedOrder);

            return errorMessage != null ? Results.BadRequest(errorMessage) : Results.Accepted($"/orders/{order!.Id}", order);
            
        }).WithName("PublishSellOrders");


        app.MapPost("orders/bids",async (HttpContext context,OrderBookService orderBookService, PlaceOrder placedOrder) =>
        {
            var correlationId = context.TraceIdentifier;
            
            var (errorMessage,order) = await orderBookService.PlaceBidAsync(correlationId, placedOrder);

            return errorMessage != null ? Results.BadRequest(errorMessage) : Results.Accepted($"/orders/{order!.Id}", order);
    
        }).WithName("PublishBuyOrders");

        app.MapGet("orders/{id}", async (OrderRepository repository, Guid id) =>
        {
            var order = await repository.GetOrderByIdAsync(id);
    
            return order == null ? Results.NotFound() : Results.Ok(order);
        }).WithName("GetOrderById");

        app.MapGet("orders/asks", async (OrderRepository repository) =>
        {
            var orders = await repository.GetActiveOrdersAsync(OrderType.Sell,SortPrice.Asc,1000);
    
            return Results.Ok(orders);
        }).WithName("GetAsks");

        app.MapGet("orders/bids", async (OrderRepository repository) =>
        {
            var orders = await repository.GetActiveOrdersAsync(OrderType.Buy, SortPrice.Desc, 1000);
    
            return Results.Ok(orders);
        }).WithName("GetBids");
        
        app.MapGet("wallets", async (OrderRepository repository) =>
        {
            var wallets = await repository.GetWalletsAsync();
    
            return Results.Ok(wallets);
        }).WithName("GetWallets");
        return app;
    }
}