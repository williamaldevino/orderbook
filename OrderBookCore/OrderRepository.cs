using Npgsql;

namespace OrderBookCore;

public enum SortPrice {Asc, Desc}

public class OrderRepository(string connectionString)
{
    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT id, wallet_code, type, price, quantity, remaining_quantity, timestamp
                  FROM orders WHERE id = @orderId limit 1;", conn);
        cmd.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Order(
                reader.GetGuid(0),
                reader.GetString(1), // Wallet Code
                (OrderType)reader.GetInt32(2), // Type
                reader.GetDecimal(3), // Price
                reader.GetInt32(4) // Quantity
            )
            {
                RemainingQuantity = reader.GetInt32(5),
                Timestamp = reader.GetDateTime(6)
            };
        }
        return null;
    }
    
    public async Task<Wallet?> GetWalletByCodeAsync(string walletCode)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT wallet_code, amount, quantity, timestamp
                  FROM wallets WHERE wallet_code = @wallet_code limit 1;", conn);
        cmd.Parameters.AddWithValue("wallet_code", walletCode);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Wallet{
               WalletCode = reader.GetString(0),
               Amount = reader.GetDecimal(1),
               Quantity = reader.GetInt32(2)
            };
        }
        return null;
    }
    
    public async Task<IEnumerable<Wallet>> GetWalletsAsync()
    {
        var wallets = new List<Wallet>();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            $@"SELECT wallet_code, amount, quantity, timestamp
                  FROM wallets;", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            wallets.Add(new Wallet{
              WalletCode   = reader.GetString(0),
              Amount  = reader.GetDecimal(1),
              Quantity  = reader.GetInt32(2)
            });
        }
        return wallets;
    }
    
    public async Task<IEnumerable<Order>> GetActiveOrdersAsync(OrderType type, SortPrice sortPrice, int limit = 100)
    {
        var orders = new List<Order>();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var sort = sortPrice == SortPrice.Asc ? "asc" : "desc";

        await using var cmd = new NpgsqlCommand(
            $@"SELECT id, wallet_code, type, price, quantity, remaining_quantity, timestamp
                  FROM orders
                  WHERE type = @type AND remaining_quantity > 0 order by price {sort}, timestamp asc limit @limit;", conn);
        
        cmd.Parameters.AddWithValue("type", (int)type);
        cmd.Parameters.AddWithValue("limit", (int)limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            orders.Add(new Order(
                reader.GetGuid(0),
                reader.GetString(1),
                (OrderType)reader.GetInt32(2),
                reader.GetDecimal(3),
                reader.GetInt32(4)
            )
            {
                RemainingQuantity = reader.GetInt32(5),
                Timestamp = reader.GetDateTime(6)
            });
        }
        return orders;
    }
    
    public async Task SaveOrderAsync(Order order)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO orders (id, wallet_code, type, price, quantity, remaining_quantity, timestamp)
                  VALUES (@id, @wallet_code, @type, @price, @quantity, @remainingQuantity, @timestamp)
                  ON CONFLICT (id) DO UPDATE SET
                    remaining_quantity = EXCLUDED.remaining_quantity,
                    timestamp = EXCLUDED.timestamp;", conn); // ON CONFLICT para upsert

        cmd.Parameters.AddWithValue("id", order.Id);
        cmd.Parameters.AddWithValue("wallet_code", order.WalletCode);
        cmd.Parameters.AddWithValue("type", (int)order.OrderType); // Armazenar enum como int
        cmd.Parameters.AddWithValue("price", order.Price);
        cmd.Parameters.AddWithValue("quantity", order.Quantity);
        cmd.Parameters.AddWithValue("remainingQuantity", order.RemainingQuantity);
        cmd.Parameters.AddWithValue("timestamp", order.Timestamp);

        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task UpdateOrderQuantityAsync(Guid orderId, decimal remainingQuantity)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"UPDATE orders SET remaining_quantity = @remainingQuantity
                  WHERE id = @orderId;", conn);

        cmd.Parameters.AddWithValue("remainingQuantity", remainingQuantity);
        cmd.Parameters.AddWithValue("orderId", orderId);

        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task UpdateWalletAsync(string walletCode, decimal amount, int quantity)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"UPDATE wallets SET quantity = @quantity, amount = @amount
                  WHERE wallet_code = @wallet_code;", conn);

        cmd.Parameters.AddWithValue("wallet_code", walletCode);
        cmd.Parameters.AddWithValue("quantity", quantity);
        cmd.Parameters.AddWithValue("amount", amount);

        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task SaveTradeAsync(Trade trade)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO trades (id, price, quantity, timestamp, buy_order_id, sell_order_id)
                  VALUES (@id, @price, @quantity, @timestamp, @buyOrderId, @sellOrderId);", conn);

        cmd.Parameters.AddWithValue("id", trade.Id);
        cmd.Parameters.AddWithValue("price", trade.Price);
        cmd.Parameters.AddWithValue("quantity", trade.Quantity);
        cmd.Parameters.AddWithValue("timestamp", trade.Timestamp);
        cmd.Parameters.AddWithValue("buyOrderId", trade.BuyOrderId);
        cmd.Parameters.AddWithValue("sellOrderId", trade.SellOrderId);

        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task<OrderBookState> LoadOrderBookStateAsync()
    {
        var buyOrders = (await GetActiveOrdersAsync(OrderType.Buy, SortPrice.Asc)).ToList();
        var sellOrders = (await GetActiveOrdersAsync(OrderType.Sell,SortPrice.Desc)).ToList();
        var wallets = (await GetWalletsAsync()).ToList();

        return new OrderBookState { BuyOrders = buyOrders, SellOrders = sellOrders , Wallets = wallets };
    }
}