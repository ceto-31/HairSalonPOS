using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Models;

namespace HairSalonPOS.Services;

public class ReportService
{
    public SalesReportRow GetDailyReport(DateTime date)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.QuerySingle<SalesReportRow>(
            @"SELECT @Date AS PeriodLabel,
                     COUNT(*) AS TransactionCount,
                     ISNULL(SUM(Total), 0) AS TotalSales,
                     ISNULL(AVG(Total), 0) AS AverageSale
              FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(@Date AS DATE)",
            new { Date = date });
    }

    public SalesReportRow GetWeeklyReport(DateTime dateInWeek)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.QuerySingle<SalesReportRow>(
            @"SELECT CONCAT('Week ', DATEPART(WEEK, @Date), ' ', YEAR(@Date)) AS PeriodLabel,
                     COUNT(*) AS TransactionCount,
                     ISNULL(SUM(Total), 0) AS TotalSales,
                     ISNULL(AVG(Total), 0) AS AverageSale
              FROM Sales
              WHERE DATEPART(WEEK, SaleDate) = DATEPART(WEEK, @Date)
                AND YEAR(SaleDate) = YEAR(@Date)",
            new { Date = dateInWeek });
    }

    public SalesReportRow GetMonthlyReport(int year, int month)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.QuerySingle<SalesReportRow>(
            @"SELECT DATENAME(MONTH, DATEFROMPARTS(@Year, @Month, 1)) + ' ' + CAST(@Year AS NVARCHAR) AS PeriodLabel,
                     COUNT(*) AS TransactionCount,
                     ISNULL(SUM(Total), 0) AS TotalSales,
                     ISNULL(AVG(Total), 0) AS AverageSale
              FROM Sales WHERE YEAR(SaleDate) = @Year AND MONTH(SaleDate) = @Month",
            new { Year = year, Month = month });
    }

    public SalesReportRow GetAnnualReport(int year)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.QuerySingle<SalesReportRow>(
            @"SELECT CAST(@Year AS NVARCHAR) AS PeriodLabel,
                     COUNT(*) AS TransactionCount,
                     ISNULL(SUM(Total), 0) AS TotalSales,
                     ISNULL(AVG(Total), 0) AS AverageSale
              FROM Sales WHERE YEAR(SaleDate) = @Year",
            new { Year = year });
    }

    public IEnumerable<TopProductRow> GetTopProducts(DateTime? from = null, DateTime? to = null, int top = 10)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var sql = $@"SELECT TOP ({top}) p.Name AS ProductName,
                            SUM(si.Quantity) AS TotalQty,
                            SUM(si.LineTotal) AS TotalRevenue
                     FROM SaleItems si
                     INNER JOIN Products p ON si.ProductId = p.ProductId
                     INNER JOIN Sales s ON si.SaleId = s.SaleId
                     WHERE 1=1";
        if (from.HasValue) sql += " AND s.SaleDate >= @From";
        if (to.HasValue) sql += " AND s.SaleDate < DATEADD(day, 1, @To)";
        sql += " GROUP BY p.Name ORDER BY TotalRevenue DESC";
        return conn.Query<TopProductRow>(sql, new { From = from, To = to });
    }
}
