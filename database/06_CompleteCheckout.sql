USE [HairSalon Db];
GO

CREATE OR ALTER PROCEDURE usp_CompleteCheckout
    @UserId INT,
    @ClientId INT,
    @SubTotal DECIMAL(18,2),
    @Tax DECIMAL(18,2),
    @Discount DECIMAL(18,2),
    @TipAmount DECIMAL(18,2),
    @TipStylistAmount DECIMAL(18,2),
    @TipAssistantAmount DECIMAL(18,2),
    @Total DECIMAL(18,2),
    @PaymentMethod NVARCHAR(30),
    @ReceiptDelivery NVARCHAR(20),
    @ReceiptEmail NVARCHAR(200) = NULL,
    @ItemsJson NVARCHAR(MAX),
    @SaleId INT OUTPUT,
    @LoyaltyPointsEarned INT OUTPUT,
    @ClientLoyaltyPoints INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Sales (UserId, ClientId, SubTotal, Tax, Discount, Total, PaymentMethod,
                           TipAmount, TipStylistAmount, TipAssistantAmount, ReceiptDelivery, ReceiptEmail)
        VALUES (@UserId, @ClientId, @SubTotal, @Tax, @Discount, @Total + @TipAmount, @PaymentMethod,
                @TipAmount, @TipStylistAmount, @TipAssistantAmount, @ReceiptDelivery, @ReceiptEmail);

        SET @SaleId = SCOPE_IDENTITY();

        DECLARE @Items TABLE (
            ProductId INT,
            Quantity INT,
            UnitPrice DECIMAL(18,2),
            LineTotal DECIMAL(18,2),
            StylistId INT,
            AssistantId INT,
            RebookInWeeks INT,
            RebookRequested BIT
        );

        INSERT INTO @Items
        SELECT
            CAST(JSON_VALUE(value, '$.ProductId') AS INT),
            CAST(JSON_VALUE(value, '$.Quantity') AS INT),
            CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2)),
            CAST(JSON_VALUE(value, '$.LineTotal') AS DECIMAL(18,2)),
            CAST(JSON_VALUE(value, '$.StylistId') AS INT),
            CAST(JSON_VALUE(value, '$.AssistantId') AS INT),
            CAST(JSON_VALUE(value, '$.RebookInWeeks') AS INT),
            ISNULL(CAST(JSON_VALUE(value, '$.RebookRequested') AS BIT), 0)
        FROM OPENJSON(@ItemsJson);

        INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, LineTotal, StylistId, AssistantId, RebookInWeeks, RebookRequested)
        SELECT @SaleId, ProductId, Quantity, UnitPrice, LineTotal, StylistId, AssistantId, RebookInWeeks, RebookRequested
        FROM @Items;

        DECLARE @ProductId INT, @Qty INT, @IsService BIT, @OnHand INT, @StockType NVARCHAR(20);

        DECLARE item_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT i.ProductId, i.Quantity, p.IsService, ISNULL(p.StockType, 'Retail')
            FROM @Items i
            INNER JOIN Products p ON p.ProductId = i.ProductId;

        OPEN item_cursor;
        FETCH NEXT FROM item_cursor INTO @ProductId, @Qty, @IsService, @StockType;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @IsService = 0 AND @StockType = 'Retail'
            BEGIN
                SELECT @OnHand = QuantityOnHand FROM Inventory WITH (UPDLOCK, ROWLOCK)
                WHERE ProductId = @ProductId;

                IF @OnHand IS NULL OR @OnHand < @Qty
                    RAISERROR('Insufficient stock for product ID %d.', 16, 1, @ProductId);

                UPDATE Inventory
                SET QuantityOnHand = QuantityOnHand - @Qty, LastUpdated = GETDATE()
                WHERE ProductId = @ProductId;

                INSERT INTO InventoryTransactions (ProductId, ChangeQty, TransactionType, ReferenceId, UserId, Notes)
                VALUES (@ProductId, -@Qty, 'Sale', @SaleId, @UserId, 'Checkout sale');
            END

            FETCH NEXT FROM item_cursor INTO @ProductId, @Qty, @IsService, @StockType;
        END

        CLOSE item_cursor;
        DEALLOCATE item_cursor;

        SET @LoyaltyPointsEarned = CAST(FLOOR((@SubTotal + @Tax - @Discount) / 10) AS INT);

        UPDATE Clients
        SET LoyaltyPoints = LoyaltyPoints + @LoyaltyPointsEarned,
            Email = CASE WHEN @ReceiptEmail IS NOT NULL AND @ReceiptEmail <> '' THEN @ReceiptEmail ELSE Email END
        WHERE ClientId = @ClientId;

        UPDATE Sales SET LoyaltyPointsEarned = @LoyaltyPointsEarned WHERE SaleId = @SaleId;

        SELECT @ClientLoyaltyPoints = LoyaltyPoints FROM Clients WHERE ClientId = @ClientId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'usp_CompleteCheckout created successfully.';
GO
