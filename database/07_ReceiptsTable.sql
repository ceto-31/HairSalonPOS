-- Official Receipt (OR) number tracking — numbers never repeat across restarts
USE [HairSalon Db];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Receipts')
BEGIN
    CREATE TABLE Receipts (
        ReceiptId     INT IDENTITY(1,1) PRIMARY KEY,
        OrNumber      NVARCHAR(20)  NOT NULL,
        SaleId        INT           NULL,
        IssuedAt      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CashierName   NVARCHAR(100) NOT NULL,
        CustomerName  NVARCHAR(100) NULL,
        StylistName   NVARCHAR(100) NULL,
        SubTotal      DECIMAL(18,2) NOT NULL,
        Discount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        Tax           DECIMAL(18,2) NOT NULL DEFAULT 0,
        Total         DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(20)  NOT NULL,
        ReceiptJson   NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_Receipts_OrNumber UNIQUE (OrNumber)
    );
    CREATE INDEX IX_Receipts_IssuedAt ON Receipts(IssuedAt DESC);
END
GO
