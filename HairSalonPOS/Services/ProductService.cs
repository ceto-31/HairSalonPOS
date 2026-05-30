using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Models;

namespace HairSalonPOS.Services;

public class ProductService
{
    public IEnumerable<Category> GetCategories()
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.Query<Category>("SELECT CategoryId, CategoryName, IsActive FROM Categories WHERE IsActive = 1 ORDER BY CategoryName");
    }

    public IEnumerable<Product> GetProducts(bool? isService = null, string? search = null)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        var sql = @"SELECT p.ProductId, p.CategoryId, c.CategoryName, p.Name, p.Price, p.IsService,
                           p.ReorderLevel, p.IsActive, ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                    LEFT JOIN Inventory i ON p.ProductId = i.ProductId
                    WHERE p.IsActive = 1";

        if (isService.HasValue)
            sql += " AND p.IsService = @IsService";
        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND p.Name LIKE '%' + @Search + '%'";

        sql += " ORDER BY p.Name";
        return conn.Query<Product>(sql, new { IsService = isService, Search = search });
    }

    public Product? GetProduct(int productId)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.QuerySingleOrDefault<Product>(
            @"SELECT p.ProductId, p.CategoryId, c.CategoryName, p.Name, p.Price, p.IsService,
                     p.ReorderLevel, p.IsActive, ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand
              FROM Products p INNER JOIN Categories c ON p.CategoryId = c.CategoryId
              LEFT JOIN Inventory i ON p.ProductId = i.ProductId
              WHERE p.ProductId = @ProductId",
            new { ProductId = productId });
    }

    public void SaveProduct(Product product)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        if (product.ProductId == 0)
        {
            product.ProductId = conn.ExecuteScalar<int>(
                @"INSERT INTO Products (CategoryId, Name, Price, IsService, ReorderLevel, IsActive)
                  VALUES (@CategoryId, @Name, @Price, @IsService, @ReorderLevel, 1);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                product, tx);

            if (!product.IsService)
            {
                conn.Execute(
                    "INSERT INTO Inventory (ProductId, QuantityOnHand) VALUES (@ProductId, 0)",
                    new { product.ProductId }, tx);
            }
        }
        else
        {
            conn.Execute(
                @"UPDATE Products SET CategoryId=@CategoryId, Name=@Name, Price=@Price,
                  IsService=@IsService, ReorderLevel=@ReorderLevel WHERE ProductId=@ProductId",
                product, tx);
        }

        tx.Commit();
    }

    public void DeleteProduct(int productId)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Execute("UPDATE Products SET IsActive = 0 WHERE ProductId = @ProductId", new { ProductId = productId });
    }
}
