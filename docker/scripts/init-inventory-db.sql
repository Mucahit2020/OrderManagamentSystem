-- Inventory Database Initialization
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create Products table
CREATE TABLE IF NOT EXISTS Products (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Price DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
    StockQuantity INTEGER NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create StockMovements table for audit trail
CREATE TABLE IF NOT EXISTS StockMovements (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ProductId UUID NOT NULL REFERENCES Products(Id),
    OrderId UUID,  -- Can be null for manual adjustments
    MovementType VARCHAR(50) NOT NULL, -- 'RESERVED', 'CONSUMED', 'RELEASED', 'MANUAL_ADJUSTMENT'
    Quantity INTEGER NOT NULL,
    PreviousQuantity INTEGER NOT NULL,
    NewQuantity INTEGER NOT NULL,
    Reason VARCHAR(500),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255) DEFAULT 'SYSTEM'
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);
CREATE INDEX IF NOT EXISTS idx_products_is_active ON Products(IsActive);
CREATE INDEX IF NOT EXISTS idx_stock_movements_product_id ON StockMovements(ProductId);
CREATE INDEX IF NOT EXISTS idx_stock_movements_order_id ON StockMovements(OrderId);
CREATE INDEX IF NOT EXISTS idx_stock_movements_type ON StockMovements(MovementType);
CREATE INDEX IF NOT EXISTS idx_stock_movements_created_at ON StockMovements(CreatedAt);

-- Create trigger for Products table
CREATE TRIGGER update_products_updated_at 
    BEFORE UPDATE ON Products 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- Insert sample products for testing
INSERT INTO Products (Id, Name, Description, Price, StockQuantity) 
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'iPhone 15 Pro', 'Apple iPhone 15 Pro 128GB', 999.99, 50),
    ('22222222-2222-2222-2222-222222222222', 'Samsung Galaxy S24', 'Samsung Galaxy S24 Ultra 256GB', 1199.99, 30),
    ('33333333-3333-3333-3333-333333333333', 'MacBook Pro M3', 'MacBook Pro 14" M3 512GB', 1999.99, 20),
    ('44444444-4444-4444-4444-444444444444', 'AirPods Pro', 'Apple AirPods Pro 2nd Gen', 249.99, 100),
    ('55555555-5555-5555-5555-555555555555', 'Dell XPS 13', 'Dell XPS 13 Laptop Intel i7', 1299.99, 15)
ON CONFLICT (Id) DO NOTHING;

-- ====================================
