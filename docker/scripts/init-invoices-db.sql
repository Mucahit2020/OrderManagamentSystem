 
-- Invoices Database Initialization  
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create Invoices table
CREATE TABLE IF NOT EXISTS Invoices (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrderId UUID UNIQUE NOT NULL,
    InvoiceNumber VARCHAR(100) UNIQUE NOT NULL,
    CustomerId UUID NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount >= 0),
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    ExternalInvoiceId VARCHAR(255), -- From external service
    ExternalReference VARCHAR(255),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ProcessedAt TIMESTAMPTZ,
    FailureReason TEXT
);

-- Create InvoiceItems table
CREATE TABLE IF NOT EXISTS InvoiceItems (
    Id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    InvoiceId UUID NOT NULL REFERENCES Invoices(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL,
    ProductName VARCHAR(255) NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    TotalPrice DECIMAL(18,2) GENERATED ALWAYS AS (Quantity * UnitPrice) STORED,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_invoices_order_id ON Invoices(OrderId);
CREATE INDEX IF NOT EXISTS idx_invoices_invoice_number ON Invoices(InvoiceNumber);  
CREATE INDEX IF NOT EXISTS idx_invoices_customer_id ON Invoices(CustomerId);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON Invoices(Status);
CREATE INDEX IF NOT EXISTS idx_invoices_created_at ON Invoices(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_invoice_items_invoice_id ON InvoiceItems(InvoiceId);

-- Create trigger for Invoices table
CREATE TRIGGER update_invoices_updated_at 
    BEFORE UPDATE ON Invoices 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- Create sequence for invoice numbers
CREATE SEQUENCE IF NOT EXISTS invoice_number_seq START 1000;

-- Create function to generate invoice number
CREATE OR REPLACE FUNCTION generate_invoice_number()
RETURNS VARCHAR(100) AS $$
BEGIN
    RETURN 'INV-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(nextval('invoice_number_seq')::text, 6, '0');
END;
$$ LANGUAGE plpgsql;