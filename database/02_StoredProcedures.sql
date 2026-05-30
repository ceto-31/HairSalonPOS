USE [HairSalon Db];
GO

CREATE OR ALTER PROCEDURE usp_ProcessSale
    @UserId INT,
    @SubTotal DECIMAL(18,2),
    @Tax DECIMAL(18,2),
    @Discount DECIMAL(18,2),
    @Total DECIMAL(18,2),
    @PaymentMethod NVARCHAR(20),
    @ItemsJson NVARCHAR(MAX),
    @SaleId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Sales (UserId, SubTotal, Tax, Discount, Total, PaymentMethod)
        VALUES (@UserId, @SubTotal, @Tax, @Discount, @Total, @PaymentMethod);

        SET @SaleId = SCOPE_IDENTITY();

        DECLARE @Items TABLE (
            ProductId INT,
            Quantity INT,
            UnitPrice DECIMAL(18,2),
            LineTotal DECIMAL(18,2)
        );

        INSERT INTO @Items (ProductId, Quantity, UnitPrice, LineTotal)
        SELECT
            CAST(JSON_VALUE(value, '$.ProductId') AS INT),
            CAST(JSON_VALUE(value, '$.Quantity') AS INT),
            CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2)),
            CAST(JSON_VALUE(value, '$.LineTotal') AS DECIMAL(18,2))
        FROM OPENJSON(@ItemsJson);

        INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, LineTotal)
        SELECT @SaleId, ProductId, Quantity, UnitPrice, LineTotal FROM @Items;

        DECLARE @ProductId INT, @Qty INT, @IsService BIT, @OnHand INT;

        DECLARE item_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT i.ProductId, i.Quantity, p.IsService
            FROM @Items i
            INNER JOIN Products p ON p.ProductId = i.ProductId;

        OPEN item_cursor;
        FETCH NEXT FROM item_cursor INTO @ProductId, @Qty, @IsService;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @IsService = 0
            BEGIN
                SELECT @OnHand = QuantityOnHand FROM Inventory WITH (UPDLOCK, ROWLOCK)
                WHERE ProductId = @ProductId;

                IF @OnHand IS NULL OR @OnHand < @Qty
                BEGIN
                    RAISERROR('Insufficient stock for product ID %d.', 16, 1, @ProductId);
                END

                UPDATE Inventory
                SET QuantityOnHand = QuantityOnHand - @Qty,
                    LastUpdated = GETDATE()
                WHERE ProductId = @ProductId;

                INSERT INTO InventoryTransactions (ProductId, ChangeQty, TransactionType, ReferenceId, UserId, Notes)
                VALUES (@ProductId, -@Qty, 'Sale', @SaleId, @UserId, 'POS sale');
            END

            FETCH NEXT FROM item_cursor INTO @ProductId, @Qty, @IsService;
        END

        CLOSE item_cursor;
        DEALLOCATE item_cursor;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE usp_RestockInventory
    @ProductId INT,
    @Quantity INT,
    @UserId INT,
    @Notes NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity <= 0
        THROW 50001, 'Restock quantity must be positive.', 1;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductId)
    BEGIN
        INSERT INTO Inventory (ProductId, QuantityOnHand) VALUES (@ProductId, @Quantity);
    END
    ELSE
    BEGIN
        UPDATE Inventory
        SET QuantityOnHand = QuantityOnHand + @Quantity,
            LastUpdated = GETDATE()
        WHERE ProductId = @ProductId;
    END

    INSERT INTO InventoryTransactions (ProductId, ChangeQty, TransactionType, UserId, Notes)
    VALUES (@ProductId, @Quantity, 'Restock', @UserId, @Notes);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE usp_AdjustInventory
    @ProductId INT,
    @NewQuantity INT,
    @UserId INT,
    @Notes NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NewQuantity < 0
        THROW 50002, 'Quantity cannot be negative.', 1;

    DECLARE @OldQty INT;
    SELECT @OldQty = QuantityOnHand FROM Inventory WHERE ProductId = @ProductId;

    IF @OldQty IS NULL
        THROW 50003, 'Product has no inventory record.', 1;

    BEGIN TRANSACTION;

    UPDATE Inventory
    SET QuantityOnHand = @NewQuantity,
        LastUpdated = GETDATE()
    WHERE ProductId = @ProductId;

    INSERT INTO InventoryTransactions (ProductId, ChangeQty, TransactionType, UserId, Notes)
    VALUES (@ProductId, @NewQuantity - @OldQty, 'Adjustment', @UserId, @Notes);

    COMMIT TRANSACTION;
END
GO

PRINT 'Stored procedures created successfully.';
GO
