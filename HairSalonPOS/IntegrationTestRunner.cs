using HairSalonPOS.Helpers;
using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS;

/// <summary>Run with: dotnet run --project HairSalonPOS -- --test</summary>
public static class IntegrationTestRunner
{
    public static bool Run()
    {
        var passed = 0;
        var failed = 0;

        void Ok(string name) { passed++; Console.WriteLine($"  PASS: {name}"); }
        void Fail(string name, string msg) { failed++; Console.WriteLine($"  FAIL: {name} - {msg}"); }

        Console.WriteLine("Hair Salon POS Integration Tests");
        Console.WriteLine("=================================");

        try
        {
            AuthService.EnsureDefaultPasswords();
            var auth = new AuthService();

            var admin = auth.Authenticate("admin", "Admin@123");
            if (admin?.RoleName == "Admin") Ok("1. Admin login"); else Fail("1. Admin login", "Invalid credentials or role");

            var bad = auth.Authenticate("admin", "wrong");
            if (bad == null) Ok("1b. Invalid login rejected"); else Fail("1b. Invalid login rejected", "Should be null");

            var productService = new ProductService();
            var products = productService.GetProducts(isService: false).ToList();
            if (products.Count >= 4) Ok("2. Products loaded"); else Fail("2. Products loaded", $"Expected >=4, got {products.Count}");

            var inventoryService = new InventoryService();
            var lowCount = inventoryService.GetLowStockCount();
            if (lowCount >= 0) Ok("12. Low stock query works"); else Fail("12. Low stock query", "Failed");

            SessionContext.SetUser(admin!);
            var salesService = new SalesService();
            var shampoo = products.First(p => p.Name.Contains("Shampoo"));
            var haircut = productService.GetProducts(isService: true).First();

            var cart = new List<CartItem>
            {
                new() { ProductId = haircut.ProductId, Name = haircut.Name, UnitPrice = haircut.Price, Quantity = 1, IsService = true },
                new() { ProductId = shampoo.ProductId, Name = shampoo.Name, UnitPrice = shampoo.Price, Quantity = 1, IsService = false }
            };

            var qtyBefore = inventoryService.GetInventory().First(i => i.ProductId == shampoo.ProductId).QuantityOnHand;
            var saleId = salesService.ProcessSale(admin!.UserId, cart, "Cash");
            var qtyAfter = inventoryService.GetInventory().First(i => i.ProductId == shampoo.ProductId).QuantityOnHand;

            if (saleId > 0) Ok("4/6/7. Sale processed"); else Fail("4/6/7. Sale processed", "SaleId=0");
            if (qtyAfter == qtyBefore - 1) Ok("6. Inventory decremented"); else Fail("6. Inventory decremented", $"{qtyBefore}->{qtyAfter}");

            var receipt = salesService.GetReceipt(saleId);
            if (receipt.Items.Count == 2 && receipt.Header.Total > 0) Ok("5. Receipt data"); else Fail("5. Receipt data", "Invalid receipt");

            var reportService = new ReportService();
            var daily = reportService.GetDailyReport(DateTime.Today);
            if (daily.TransactionCount >= 1) Ok("8. Daily report"); else Fail("8. Daily report", "No transactions");

            var weekly = reportService.GetWeeklyReport(DateTime.Today);
            var monthly = reportService.GetMonthlyReport(DateTime.Today.Year, DateTime.Today.Month);
            var annual = reportService.GetAnnualReport(DateTime.Today.Year);
            if (weekly.TotalSales >= daily.TotalSales) Ok("8b. Weekly report"); else Fail("8b. Weekly report", "Totals mismatch");
            if (monthly.TotalSales >= daily.TotalSales) Ok("8c. Monthly report"); else Fail("8c. Monthly report", "Totals mismatch");
            if (annual.TotalSales >= daily.TotalSales) Ok("8d. Annual report"); else Fail("8d. Annual report", "Totals mismatch");

            var stockReport = inventoryService.GetInventory().ToList();
            if (stockReport.Count >= 4) Ok("9. Inventory report"); else Fail("9. Inventory report", "Insufficient rows");

            var userService = new UserService();
            var users = userService.GetAllUsers().ToList();
            if (users.Count >= 3) Ok("10. User management data"); else Fail("10. User management data", $"Count={users.Count}");

            var backupService = new BackupService();
            var backupPath = backupService.CreateBackup(admin.UserId);
            if (File.Exists(backupPath)) Ok("11. Database backup"); else Fail("11. Database backup", "File not found");

            SessionContext.Clear();
        }
        catch (Exception ex)
        {
            Fail("EXCEPTION", ex.Message);
        }

        Console.WriteLine($"=================================");
        Console.WriteLine($"Results: {passed} passed, {failed} failed");
        return failed == 0;
    }
}
