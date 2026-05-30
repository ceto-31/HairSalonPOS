using System.Text.Json;
using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Models;
using Microsoft.Data.SqlClient;

namespace HairSalonPOS.Services;

public class SalesService
{
    private readonly decimal _taxRate;

    public SalesService()
    {
        var taxSetting = System.Configuration.ConfigurationManager.AppSettings["TaxRate"];
        _taxRate = decimal.TryParse(taxSetting, out var rate) ? rate : 0.12m;
    }

    public decimal TaxRate => _taxRate;

    public int ProcessSale(int userId, List<CartItem> items, string paymentMethod, decimal discount = 0)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var subTotal = items.Sum(i => i.LineTotal);
        var tax = Math.Round(subTotal * _taxRate, 2);
        var total = subTotal + tax - discount;

        var jsonItems = JsonSerializer.Serialize(items.Select(i => new
        {
            i.ProductId,
            i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        }));

        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("usp_ProcessSale", conn) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@SubTotal", subTotal);
        cmd.Parameters.AddWithValue("@Tax", tax);
        cmd.Parameters.AddWithValue("@Discount", discount);
        cmd.Parameters.AddWithValue("@Total", total);
        cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
        cmd.Parameters.AddWithValue("@ItemsJson", jsonItems);
        var saleIdParam = cmd.Parameters.Add("@SaleId", System.Data.SqlDbType.Int);
        saleIdParam.Direction = System.Data.ParameterDirection.Output;
        cmd.ExecuteNonQuery();
        return (int)saleIdParam.Value;
    }

    public SaleReceipt GetReceipt(int saleId)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var header = conn.QuerySingle<SaleRecord>(
            @"SELECT s.SaleId, s.SaleDate, u.FullName AS CashierName, s.SubTotal, s.Tax, s.Discount, s.Total, s.PaymentMethod
              FROM Sales s INNER JOIN Users u ON s.UserId = u.UserId WHERE s.SaleId = @SaleId",
            new { SaleId = saleId });

        var items = conn.Query<SaleItemRecord>(
            @"SELECT p.Name AS ProductName, si.Quantity, si.UnitPrice, si.LineTotal
              FROM SaleItems si INNER JOIN Products p ON si.ProductId = p.ProductId
              WHERE si.SaleId = @SaleId",
            new { SaleId = saleId }).ToList();

        return new SaleReceipt { Header = header, Items = items };
    }

    public IEnumerable<SaleRecord> GetSalesHistory(DateTime? from = null, DateTime? to = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var sql = @"SELECT s.SaleId, s.SaleDate, u.FullName AS CashierName, s.SubTotal, s.Tax, s.Discount, s.Total, s.PaymentMethod
                    FROM Sales s INNER JOIN Users u ON s.UserId = u.UserId WHERE 1=1";
        if (from.HasValue) sql += " AND s.SaleDate >= @From";
        if (to.HasValue) sql += " AND s.SaleDate < DATEADD(day, 1, @To)";
        sql += " ORDER BY s.SaleDate DESC";
        return conn.Query<SaleRecord>(sql, new { From = from, To = to });
    }
}
