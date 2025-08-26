-- Orders Database Initialization
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create Orders table
CREATE TABLE IF NOT EXISTS Orders (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    IdempotencyKey VARCHAR(255) UNIQUE NOT NULL,
    CustomerId UUID NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status VARCHAR(50) NOT NULL DEFAULT 'Created',
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create OrderItems table  
CREATE TABLE IF NOT EXISTS OrderItems (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrderId UUID NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL,
    ProductName VARCHAR(255) NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    TotalPrice DECIMAL(18,2) GENERATED ALWAYS AS (Quantity * UnitPrice) STORED,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_orders_idempotency_key ON Orders(IdempotencyKey);
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON Orders(CustomerId);
CREATE INDEX IF NOT EXISTS idx_orders_status ON Orders(Status);
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON Orders(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON OrderItems(OrderId);
CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON OrderItems(ProductId);

-- Create audit trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for Orders table
CREATE TRIGGER update_orders_updated_at 
    BEFORE UPDATE ON Orders 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- ====================================