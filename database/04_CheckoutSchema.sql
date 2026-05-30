USE [HairSalon Db];
GO

-- Extend Products table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Sku')
    ALTER TABLE Products ADD Sku NVARCHAR(20) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Brand')
    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Supplier')
    ALTER TABLE Products ADD Supplier NVARCHAR(150) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'DurationMinutes')
    ALTER TABLE Products ADD DurationMinutes INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'StockType')
    ALTER TABLE Products ADD StockType NVARCHAR(20) NULL DEFAULT 'Retail';
GO

-- Extend Sales table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'ClientId')
    ALTER TABLE Sales ADD ClientId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TipAmount')
    ALTER TABLE Sales ADD TipAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TipStylistAmount')
    ALTER TABLE Sales ADD TipStylistAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TipAssistantAmount')
    ALTER TABLE Sales ADD TipAssistantAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'ReceiptDelivery')
    ALTER TABLE Sales ADD ReceiptDelivery NVARCHAR(20) NULL DEFAULT 'Print';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'ReceiptEmail')
    ALTER TABLE Sales ADD ReceiptEmail NVARCHAR(200) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'LoyaltyPointsEarned')
    ALTER TABLE Sales ADD LoyaltyPointsEarned INT NOT NULL DEFAULT 0;
GO

-- Extend SaleItems table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'StylistId')
    ALTER TABLE SaleItems ADD StylistId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'AssistantId')
    ALTER TABLE SaleItems ADD AssistantId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'RebookInWeeks')
    ALTER TABLE SaleItems ADD RebookInWeeks INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'RebookRequested')
    ALTER TABLE SaleItems ADD RebookRequested BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        ClientId      INT IDENTITY(1,1) PRIMARY KEY,
        FirstName     NVARCHAR(100) NOT NULL,
        LastName      NVARCHAR(100) NOT NULL,
        Email         NVARCHAR(200) NULL,
        LoyaltyPoints INT NOT NULL DEFAULT 0,
        LoyaltyGoal   INT NOT NULL DEFAULT 500,
        IsActive      BIT NOT NULL DEFAULT 1,
        CreatedAt     DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Staff')
BEGIN
    CREATE TABLE Staff (
        StaffId   INT IDENTITY(1,1) PRIMARY KEY,
        FullName  NVARCHAR(100) NOT NULL,
        StaffRole NVARCHAR(30) NOT NULL CHECK (StaffRole IN ('Stylist', 'Assistant')),
        IsActive  BIT NOT NULL DEFAULT 1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductRecommendations')
BEGIN
    CREATE TABLE ProductRecommendations (
        RecommendationId   INT IDENTITY(1,1) PRIMARY KEY,
        ServiceProductId   INT NOT NULL REFERENCES Products(ProductId),
        RecommendedProductId INT NOT NULL REFERENCES Products(ProductId),
        Message            NVARCHAR(255) NOT NULL,
        IsActive           BIT NOT NULL DEFAULT 1
    );
END
GO

-- Add FK for Sales.ClientId after Clients exists
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Sales_Clients')
    ALTER TABLE Sales ADD CONSTRAINT FK_Sales_Clients FOREIGN KEY (ClientId) REFERENCES Clients(ClientId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SaleItems_Stylist')
    ALTER TABLE SaleItems ADD CONSTRAINT FK_SaleItems_Stylist FOREIGN KEY (StylistId) REFERENCES Staff(StaffId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SaleItems_Assistant')
    ALTER TABLE SaleItems ADD CONSTRAINT FK_SaleItems_Assistant FOREIGN KEY (AssistantId) REFERENCES Staff(StaffId);
GO

-- Seed default client
IF NOT EXISTS (SELECT 1 FROM Clients)
BEGIN
    INSERT INTO Clients (FirstName, LastName, Email, LoyaltyPoints, LoyaltyGoal)
    VALUES ('Walk-in', 'Guest', NULL, 450, 500);
END
GO

-- Seed staff
IF NOT EXISTS (SELECT 1 FROM Staff)
BEGIN
    INSERT INTO Staff (FullName, StaffRole) VALUES
        ('Ana Reyes', 'Stylist'),
        ('Maria Santos', 'Stylist'),
        ('Jessa Cruz', 'Stylist'),
        ('Liza Gomez', 'Assistant');
END
GO

PRINT 'Checkout schema extensions applied successfully.';
GO
