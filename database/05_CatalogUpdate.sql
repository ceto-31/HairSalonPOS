USE [HairSalon Db];
GO

-- Clear old sale data that references products (optional fresh catalog)
-- Keep sales history: deactivate old products instead of deleting
UPDATE Products SET IsActive = 0 WHERE Sku IS NULL OR Sku NOT IN ('P001','P002','P003','P004','P005','S001','S002','S003','S004','S005');
GO

-- Ensure categories
MERGE Categories AS t
USING (VALUES
    ('Hair Care'),
    ('Hair Treatment'),
    ('Haircut'),
    ('Color'),
    ('Spa')
) AS s(CategoryName) ON t.CategoryName = s.CategoryName
WHEN NOT MATCHED THEN INSERT (CategoryName) VALUES (s.CategoryName);
GO

DECLARE @HairCare INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Hair Care');
DECLARE @HairTreatment INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Hair Treatment');
DECLARE @Haircut INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Haircut');
DECLARE @Color INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Color');
DECLARE @Spa INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Spa');

-- Services S001-S005
MERGE Products AS t
USING (VALUES
    ('S001', @Haircut, 'Haircut', 150.00, 1, 0, NULL, NULL, 30, NULL),
    ('S002', @Color, 'Hair Coloring', 800.00, 1, 0, NULL, NULL, 120, NULL),
    ('S003', @HairTreatment, 'Hair Rebond', 2500.00, 1, 0, NULL, NULL, 240, NULL),
    ('S004', @HairTreatment, 'Hair Treatment', 500.00, 1, 0, NULL, NULL, 60, NULL),
    ('S005', @Spa, 'Hair Spa', 600.00, 1, 0, NULL, NULL, 60, NULL)
) AS s(Sku, CategoryId, Name, Price, IsService, ReorderLevel, Brand, Supplier, DurationMinutes, StockType)
ON t.Sku = s.Sku
WHEN MATCHED THEN UPDATE SET
    CategoryId = s.CategoryId, Name = s.Name, Price = s.Price, IsService = s.IsService,
    ReorderLevel = s.ReorderLevel, DurationMinutes = s.DurationMinutes, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Sku, CategoryId, Name, Price, IsService, ReorderLevel, Brand, Supplier, DurationMinutes, StockType, IsActive)
VALUES (s.Sku, s.CategoryId, s.Name, s.Price, s.IsService, s.ReorderLevel, s.Brand, s.Supplier, s.DurationMinutes, s.StockType, 1);
GO

-- Retail products P001-P005
DECLARE @HairCare2 INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Hair Care');
DECLARE @HairTreatment2 INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Hair Treatment');

MERGE Products AS t
USING (VALUES
    ('P001', @HairCare2, 'Shampoo 500ml', 250.00, 0, 10, 'Dove', 'Beauty Essentials Trading', NULL, 'Retail'),
    ('P002', @HairCare2, 'Conditioner 500ml', 250.00, 0, 10, 'Dove', 'Beauty Essentials Trading', NULL, 'Retail'),
    ('P003', @HairTreatment2, 'Hair Color Black', 350.00, 0, 8, 'Revlon', 'Salon Supply Center', NULL, 'Backbar'),
    ('P004', @HairTreatment2, 'Hair Color Brown', 350.00, 0, 8, 'Revlon', 'Salon Supply Center', NULL, 'Backbar'),
    ('P005', @HairCare2, 'Hair Serum', 180.00, 0, 10, 'Vitress', 'Beauty Essentials Trading', NULL, 'Retail')
) AS s(Sku, CategoryId, Name, Price, IsService, ReorderLevel, Brand, Supplier, DurationMinutes, StockType)
ON t.Sku = s.Sku
WHEN MATCHED THEN UPDATE SET
    CategoryId = s.CategoryId, Name = s.Name, Price = s.Price, IsService = s.IsService,
    ReorderLevel = s.ReorderLevel, Brand = s.Brand, Supplier = s.Supplier, StockType = s.StockType, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Sku, CategoryId, Name, Price, IsService, ReorderLevel, Brand, Supplier, DurationMinutes, StockType, IsActive)
VALUES (s.Sku, s.CategoryId, s.Name, s.Price, s.IsService, s.ReorderLevel, s.Brand, s.Supplier, s.DurationMinutes, s.StockType, 1);
GO

-- Inventory for retail products
INSERT INTO Inventory (ProductId, QuantityOnHand)
SELECT p.ProductId, v.Qty
FROM Products p
INNER JOIN (VALUES
    ('P001', 50), ('P002', 45), ('P003', 30), ('P004', 25), ('P005', 40)
) AS v(Sku, Qty) ON p.Sku = v.Sku
WHERE NOT EXISTS (SELECT 1 FROM Inventory i WHERE i.ProductId = p.ProductId);
GO

UPDATE i SET QuantityOnHand = v.Qty, LastUpdated = GETDATE()
FROM Inventory i
INNER JOIN Products p ON i.ProductId = p.ProductId
INNER JOIN (VALUES
    ('P001', 50), ('P002', 45), ('P003', 30), ('P004', 25), ('P005', 40)
) AS v(Sku, Qty) ON p.Sku = v.Sku;
GO

-- Product recommendations
DECLARE @HairColorService INT = (SELECT ProductId FROM Products WHERE Sku = 'S002');
DECLARE @Shampoo INT = (SELECT ProductId FROM Products WHERE Sku = 'P001');
DECLARE @Serum INT = (SELECT ProductId FROM Products WHERE Sku = 'P005');
DECLARE @HaircutService INT = (SELECT ProductId FROM Products WHERE Sku = 'S001');
DECLARE @Conditioner INT = (SELECT ProductId FROM Products WHERE Sku = 'P002');

IF NOT EXISTS (SELECT 1 FROM ProductRecommendations)
BEGIN
    INSERT INTO ProductRecommendations (ServiceProductId, RecommendedProductId, Message) VALUES
        (@HairColorService, @Shampoo, 'Used color service? Recommend Purple Shampoo for color protection.'),
        (@HairColorService, @Serum, 'Protect your new color with Hair Serum.'),
        (@HaircutService, @Conditioner, 'Keep your fresh cut smooth with Conditioner 500ml.');
END
GO

PRINT 'Catalog updated with P001-P005 and S001-S005.';
GO
