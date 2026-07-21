USE [HairSalon Db];
GO

-- Reassign any Manager users to Cashier, deactivate legacy 'manager' login, then remove the Manager role.
-- Safe to re-run on databases that never had Manager.

DECLARE @CashierRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = N'Cashier');
DECLARE @ManagerRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = N'Manager');

IF @ManagerRoleId IS NOT NULL AND @CashierRoleId IS NOT NULL
BEGIN
    UPDATE Users
    SET RoleId = @CashierRoleId
    WHERE RoleId = @ManagerRoleId;

    DELETE FROM Roles WHERE RoleId = @ManagerRoleId;
END

UPDATE Users SET IsActive = 0 WHERE Username = N'manager';
GO

PRINT 'Manager role removed (users reassigned to Cashier if present).';
GO
