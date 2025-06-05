-- USE yugabyte; -- Não necessário ao conectar diretamente ao DB

CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY,
    wallet_code VARCHAR(50) NOT NULL,
    type INT NOT NULL, -- 0 for Buy, 1 for Sell
    price DECIMAL(20, 8) NOT NULL,
    quantity DECIMAL(20, 8) NOT NULL,
    remaining_quantity DECIMAL(20, 8) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_orders_wallet_code ON orders (wallet_code);
CREATE INDEX IF NOT EXISTS idx_orders_wallet_code_type_remaining ON orders (wallet_code,type, remaining_quantity) WHERE remaining_quantity > 0;
CREATE INDEX IF NOT EXISTS idx_orders_timestamp ON orders (timestamp DESC);

CREATE TABLE IF NOT EXISTS trades (
    id UUID PRIMARY KEY,    
    price DECIMAL(20, 8) NOT NULL,
    quantity DECIMAL(20, 8) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    buy_order_id UUID NOT NULL,
    sell_order_id UUID NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_trades_symbol_timestamp ON trades (timestamp DESC);

CREATE TABLE IF NOT EXISTS wallets (       
    wallet_code VARCHAR(50) PRIMARY KEY,
    amount DECIMAL(20, 8) NOT NULL,
    quantity DECIMAL(20, 8) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP    
);

CREATE INDEX IF NOT EXISTS idx_wallets_timestamp ON wallets (timestamp DESC);

ALTER TABLE orders
ADD CONSTRAINT fk_wallets
FOREIGN KEY (wallet_code)
REFERENCES wallets(wallet_code);
--ON DELETE CASCADE; 