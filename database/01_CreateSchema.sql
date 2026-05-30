USE [HairSalon Db];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleId   INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId       INT IDENTITY(1,1) PRIMARY KEY,
        Username     NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        FullName     NVARCHAR(100) NOT NULL,
        RoleId       INT NOT NULL REFERENCES Roles(RoleId),
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive     BIT NOT NULL DEFAULT 1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        ProductId    INT IDENTITY(1,1) PRIMARY KEY,
        CategoryId   INT NOT NULL REFERENCES Categories(CategoryId),
        Name         NVARCHAR(150) NOT NULL,
        Price        DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
        IsService    BIT NOT NULL DEFAULT 0,
        ReorderLevel INT NOT NULL DEFAULT 5,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Inventory')
BEGIN
    CREATE TABLE Inventory (
        ProductId      INT PRIMARY KEY REFERENCES Products(ProductId),
        QuantityOnHand INT NOT NULL DEFAULT 0 CHECK (QuantityOnHand >= 0),
        LastUpdated    DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Sales')
BEGIN
    CREATE TABLE Sales (
        SaleId        INT IDENTITY(1,1) PRIMARY KEY,
        SaleDate      DATETIME2 NOT NULL DEFAULT GETDATE(),
        UserId        INT NOT NULL REFERENCES Users(UserId),
        SubTotal      DECIMAL(18,2) NOT NULL,
        Tax           DECIMAL(18,2) NOT NULL DEFAULT 0,
        Discount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        Total         DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(20) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SaleItems')
BEGIN
    CREATE TABLE SaleItems (
        SaleItemId INT IDENTITY(1,1) PRIMARY KEY,
        SaleId     INT NOT NULL REFERENCES Sales(SaleId),
        ProductId  INT NOT NULL REFERENCES Products(ProductId),
        Quantity   INT NOT NULL CHECK (Quantity > 0),
        UnitPrice  DECIMAL(18,2) NOT NULL,
        LineTotal  DECIMAL(18,2) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryTransactions')
BEGIN
    CREATE TABLE InventoryTransactions (
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        ProductId     INT NOT NULL REFERENCES Products(ProductId),
        ChangeQty     INT NOT NULL,
        TransactionType NVARCHAR(30) NOT NULL,
        ReferenceId   INT NULL,
        Notes         NVARCHAR(255) NULL,
        UserId        INT NULL REFERENCES Users(UserId),
        CreatedAt     DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BackupLog')
BEGIN
    CREATE TABLE BackupLog (
        BackupLogId INT IDENTITY(1,1) PRIMARY KEY,
        BackupPath  NVARCHAR(500) NOT NULL,
        BackupDate  DATETIME2 NOT NULL DEFAULT GETDATE(),
        UserId      INT NULL REFERENCES Users(UserId),
        Notes       NVARCHAR(255) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sales_SaleDate')
    CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name')
    CREATE INDEX IX_Products_Name ON Products(Name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Inventory_QuantityOnHand')
    CREATE INDEX IX_Inventory_QuantityOnHand ON Inventory(QuantityOnHand);
GO

-- Seed roles
IF NOT EXISTS (SELECT 1 FROM Roles)
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Manager'), ('Cashier');
END
GO

-- Seed admin user (password: Admin@123)
-- Hash format: iterations.salt.base64hash (PBKDF2-SHA256)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, RoleId)
    SELECT 'admin',
           '100000.YWJjZGVmZ2g=.placeholder_will_be_set_by_app',
           'System Administrator',
           RoleId
    FROM Roles WHERE RoleName = 'Admin';
END
GO

PRINT 'Schema created successfully.';
GO
