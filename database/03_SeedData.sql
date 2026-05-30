USE [HairSalon Db];
GO

IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (CategoryName) VALUES
        ('Haircut'),
        ('Color'),
        ('Treatment'),
        ('Retail');
END
GO

IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    DECLARE @HaircutId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Haircut');
    DECLARE @ColorId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Color');
    DECLARE @TreatmentId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Treatment');
    DECLARE @RetailId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Retail');

    INSERT INTO Products (CategoryId, Name, Price, IsService, ReorderLevel) VALUES
        (@HaircutId, 'Basic Haircut', 350.00, 1, 0),
        (@HaircutId, 'Kids Haircut', 250.00, 1, 0),
        (@ColorId, 'Full Color', 1200.00, 1, 0),
        (@ColorId, 'Highlights', 1500.00, 1, 0),
        (@TreatmentId, 'Blowdry', 300.00, 1, 0),
        (@TreatmentId, 'Hair Spa', 800.00, 1, 0),
        (@RetailId, 'Shampoo 250ml', 250.00, 0, 10),
        (@RetailId, 'Conditioner 250ml', 280.00, 0, 10),
        (@RetailId, 'Hair Serum', 450.00, 0, 5),
        (@RetailId, 'Styling Gel', 180.00, 0, 8);

    INSERT INTO Inventory (ProductId, QuantityOnHand)
    SELECT ProductId, CASE Name
        WHEN 'Shampoo 250ml' THEN 25
        WHEN 'Conditioner 250ml' THEN 20
        WHEN 'Hair Serum' THEN 8
        WHEN 'Styling Gel' THEN 15
        ELSE 0 END
    FROM Products WHERE IsService = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager')
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, RoleId)
    SELECT 'manager', (SELECT PasswordHash FROM Users WHERE Username = 'admin'), 'Salon Manager', RoleId
    FROM Roles WHERE RoleName = 'Manager';
END
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'cashier')
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, RoleId)
    SELECT 'cashier', (SELECT PasswordHash FROM Users WHERE Username = 'admin'), 'Front Desk Cashier', RoleId
    FROM Roles WHERE RoleName = 'Cashier';
END
GO

PRINT 'Seed data inserted successfully.';
GO
