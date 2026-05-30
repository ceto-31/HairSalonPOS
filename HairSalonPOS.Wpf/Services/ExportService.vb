Imports System.IO
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports HairSalonPOS.Wpf.Models
Imports Microsoft.Win32

Namespace Services
    Public Class ExportService
        Public Shared Sub ExportSalesCsv(sales As IEnumerable(Of SaleRecord), filePath As String)
            Dim sb As New StringBuilder()
            sb.AppendLine("Receipt,SaleId,Date,Cashier,Customer,Stylist,Payment,SubTotal,Discount,Tax,Total")
            For Each s In sales
                sb.AppendLine($"{s.ReceiptNumber},{s.SaleId},{s.SaleDate:yyyy-MM-dd HH:mm},{s.CashierName},{s.CustomerName},{s.StylistName},{s.PaymentMethod},{s.SubTotal},{s.DiscountAmount},{s.Tax},{s.Total}")
            Next
            File.WriteAllText(filePath, sb.ToString())
        End Sub

        Public Shared Sub ExportSalesPdf(sales As IEnumerable(Of SaleRecord), summary As String)
            Dim dlg As New SaveFileDialog With {.Filter = "PDF files|*.txt", .FileName = "SalesReport.txt"}
            If dlg.ShowDialog() <> True Then Return
            File.WriteAllText(dlg.FileName, summary & Environment.NewLine & String.Join(Environment.NewLine, sales.Select(Function(s) $"{s.ReceiptNumber} | {s.SaleDate:g} | {s.Total:N2}")))
            MessageBox.Show("Report saved.", "Export", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub
    End Class

    Public Class InventoryService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Public Function SaveProduct(product As ProductItem, isNew As Boolean, userName As String) As ProductItem
            If isNew Then
                If _store.Products.Any(Function(p) p.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase)) Then
                    Throw New InvalidOperationException("SKU already exists.")
                End If
                _store.Products.Add(product)
                _store.LogMovement(product.Sku, product.StockOnHand, "Restock", userName, "Initial stock")
            Else
                Dim existing = _store.Products.First(Function(p) p.Sku = product.Sku)
                Dim delta = product.StockOnHand - existing.StockOnHand
                existing.Name = product.Name
                existing.Brand = product.Brand
                existing.Price = product.Price
                existing.Cost = product.Cost
                existing.ReorderLevel = product.ReorderLevel
                existing.StockOnHand = product.StockOnHand
                If delta <> 0 Then _store.LogMovement(product.Sku, delta, "Adjustment", userName, "Manual edit")
            End If
            Return product
        End Function

        Public Sub UpdateStockInline(sku As String, newQty As Integer, userName As String)
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            Dim delta = newQty - product.StockOnHand
            product.StockOnHand = newQty
            _store.LogMovement(sku, delta, "Adjustment", userName, "Inline qty edit")
        End Sub
    End Class
End Namespace
