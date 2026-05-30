using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Models;
using Microsoft.Data.SqlClient;

namespace HairSalonPOS.Services;

public class InventoryService
{
    public IEnumerable<InventoryRow> GetInventory(string? filter = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var sql = @"SELECT p.ProductId, p.Name AS ProductName, i.QuantityOnHand, p.ReorderLevel, i.LastUpdated
                    FROM Products p
                    INNER JOIN Inventory i ON p.ProductId = i.ProductId
                    WHERE p.IsActive = 1 AND p.IsService = 0";

        if (filter == "low")
            sql += " AND i.QuantityOnHand <= p.ReorderLevel";

        sql += " ORDER BY p.Name";
        return conn.Query<InventoryRow>(sql);
    }

    public int GetLowStockCount()
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM Products p
              INNER JOIN Inventory i ON p.ProductId = i.ProductId
              WHERE p.IsActive = 1 AND p.IsService = 0 AND i.QuantityOnHand <= p.ReorderLevel");
    }

    public void Restock(int productId, int quantity, int userId, string? notes = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("usp_RestockInventory", conn) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@Quantity", quantity);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void Adjust(int productId, int newQuantity, int userId, string? notes = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("usp_AdjustInventory", conn) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<InventoryTransaction> GetTransactionLog(DateTime? from = null, DateTime? to = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var sql = @"SELECT t.TransactionId, p.Name AS ProductName, t.ChangeQty, t.TransactionType,
                           ISNULL(t.Notes, '') AS Notes, ISNULL(u.FullName, '') AS UserName, t.CreatedAt
                    FROM InventoryTransactions t
                    INNER JOIN Products p ON t.ProductId = p.ProductId
                    LEFT JOIN Users u ON t.UserId = u.UserId
                    WHERE 1=1";

        if (from.HasValue) sql += " AND t.CreatedAt >= @From";
        if (to.HasValue) sql += " AND t.CreatedAt < DATEADD(day, 1, @To)";

        sql += " ORDER BY t.CreatedAt DESC";
        return conn.Query<InventoryTransaction>(sql, new { From = from, To = to });
    }
}
