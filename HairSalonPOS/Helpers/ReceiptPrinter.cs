using System.Configuration;
using System.Drawing.Printing;
using HairSalonPOS.Models;

namespace HairSalonPOS.Helpers;

public class ReceiptPrinter
{
    private readonly SaleReceipt _receipt;
    private readonly string _salonName;
    private readonly string _salonAddress;
    private float _y;
    private readonly Font _titleFont = new("Segoe UI", 14, FontStyle.Bold);
    private readonly Font _normalFont = new("Segoe UI", 9);
    private readonly Font _boldFont = new("Segoe UI", 9, FontStyle.Bold);

    public ReceiptPrinter(SaleReceipt receipt)
    {
        _receipt = receipt;
        _salonName = ConfigurationManager.AppSettings["SalonName"] ?? "Hair Salon";
        _salonAddress = ConfigurationManager.AppSettings["SalonAddress"] ?? "";
    }

    public void Print()
    {
        using var doc = CreatePrintDocument();
        using var preview = new PrintPreviewDialog { Document = doc, Width = 800, Height = 600 };
        preview.ShowDialog();
    }

    public void PrintDirect()
    {
        using var doc = CreatePrintDocument();
        doc.Print();
    }

    public void SaveToFile(string filePath)
    {
        using var doc = CreatePrintDocument();
        var lines = BuildReceiptLines();
        File.WriteAllLines(filePath, lines);
    }

    public PrintDocument CreatePrintDocument()
    {
        var doc = new PrintDocument();
        doc.PrintPage += (_, e) =>
        {
            _y = e.MarginBounds.Top;
            var g = e.Graphics!;
            var center = e.MarginBounds.Width / 2 + e.MarginBounds.Left;

            DrawCentered(g, _salonName, _titleFont, center);
            DrawCentered(g, _salonAddress, _normalFont, center);
            DrawCentered(g, "OFFICIAL RECEIPT", _boldFont, center);
            _y += 10;
            DrawLine(g, e.MarginBounds.Left, e.MarginBounds.Width);
            DrawText(g, $"Receipt #: {_receipt.Header.SaleId}");
            DrawText(g, $"Date: {_receipt.Header.SaleDate:yyyy-MM-dd HH:mm}");
            DrawText(g, $"Cashier: {_receipt.Header.CashierName}");
            DrawText(g, $"Payment: {_receipt.Header.PaymentMethod}");
            _y += 5;
            DrawLine(g, e.MarginBounds.Left, e.MarginBounds.Width);

            foreach (var item in _receipt.Items)
            {
                DrawText(g, $"{item.ProductName} x{item.Quantity} @ {item.UnitPrice:N2} = {item.LineTotal:N2}");
            }

            _y += 5;
            DrawLine(g, e.MarginBounds.Left, e.MarginBounds.Width);
            DrawText(g, $"Subtotal: {_receipt.Header.SubTotal:N2}");
            DrawText(g, $"Tax: {_receipt.Header.Tax:N2}");
            if (_receipt.Header.Discount > 0)
                DrawText(g, $"Discount: -{_receipt.Header.Discount:N2}");
            DrawText(g, $"TOTAL: {_receipt.Header.Total:N2}", _boldFont);
            _y += 15;
            DrawCentered(g, "Thank you for visiting!", _normalFont, center);
            DrawCentered(g, "Please come again.", _normalFont, center);
        };
        return doc;
    }

    private List<string> BuildReceiptLines()
    {
        var lines = new List<string>
        {
            _salonName, _salonAddress, "OFFICIAL RECEIPT", new string('-', 40),
            $"Receipt #: {_receipt.Header.SaleId}",
            $"Date: {_receipt.Header.SaleDate:yyyy-MM-dd HH:mm}",
            $"Cashier: {_receipt.Header.CashierName}",
            $"Payment: {_receipt.Header.PaymentMethod}",
            new string('-', 40)
        };
        lines.AddRange(_receipt.Items.Select(i => $"{i.ProductName} x{i.Quantity} @ {i.UnitPrice:N2} = {i.LineTotal:N2}"));
        lines.Add(new string('-', 40));
        lines.Add($"Subtotal: {_receipt.Header.SubTotal:N2}");
        lines.Add($"Tax: {_receipt.Header.Tax:N2}");
        lines.Add($"TOTAL: {_receipt.Header.Total:N2}");
        lines.Add("");
        lines.Add("Thank you for visiting!");
        return lines;
    }

    private void DrawText(Graphics g, string text, Font? font = null)
    {
        font ??= _normalFont;
        g.DrawString(text, font, Brushes.Black, 50, _y);
        _y += font.GetHeight(g) + 2;
    }

    private void DrawCentered(Graphics g, string text, Font font, float centerX)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.Black, centerX - size.Width / 2, _y);
        _y += font.GetHeight(g) + 2;
    }

    private void DrawLine(Graphics g, float left, float width)
    {
        g.DrawLine(Pens.Black, left, _y, left + width, _y);
        _y += 8;
    }
}
