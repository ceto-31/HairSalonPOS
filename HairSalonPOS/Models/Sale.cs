namespace HairSalonPOS.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsService { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class SaleRecord
{
    public int SaleId { get; set; }
    public DateTime SaleDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class SaleItemRecord
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class SaleReceipt
{
    public SaleRecord Header { get; set; } = new();
    public List<SaleItemRecord> Items { get; set; } = new();
}

public class SalesReportRow
{
    public string PeriodLabel { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageSale { get; set; }
}

public class TopProductRow
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public decimal TotalRevenue { get; set; }
}
