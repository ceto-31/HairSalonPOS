namespace HairSalonPOS.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Product
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsService { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public int QuantityOnHand { get; set; }
}

public class InventoryRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string Status => QuantityOnHand <= ReorderLevel ? "LOW" : "OK";
    public DateTime LastUpdated { get; set; }
}

public class InventoryTransaction
{
    public int TransactionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ChangeQty { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
