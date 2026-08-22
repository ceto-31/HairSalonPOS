Imports System.Windows
Imports HairSalonPOS.Wpf.Models
Imports Microsoft.Win32
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services
    Public Class ExportService
        Shared Sub New()
            QuestPDF.Settings.License = LicenseType.Community
        End Sub

        Public Shared Function ExportSalesPdf(sales As IEnumerable(Of SaleRecord), title As String, summaryLines As IEnumerable(Of String)) As Boolean
            Dim dlg As New SaveFileDialog With {
                .Filter = "PDF files|*.pdf",
                .FileName = "SalesReport.pdf",
                .DefaultExt = "pdf",
                .AddExtension = True
            }
            If dlg.ShowDialog() <> True Then Return False

            Dim saleList = sales.ToList()
            Dim lines = If(summaryLines, Enumerable.Empty(Of String)()).ToList()

            Document.Create(
                Sub(container)
                    container.Page(
                        Sub(page)
                            page.Margin(40)
                            page.Size(PageSizes.A4)

                            page.Header().Column(
                                Sub(col)
                                    col.Item().Text(title).FontSize(18).SemiBold()
                                    col.Item().PaddingTop(4).Text($"Generated: {DateTime.Now:g}").FontSize(9).FontColor(Colors.Grey.Darken1)
                                    For Each line In lines
                                        col.Item().Text(line).FontSize(10)
                                    Next
                                End Sub)

                            page.Content().PaddingTop(16).Table(
                                Sub(table)
                                    table.ColumnsDefinition(
                                        Sub(columns)
                                            columns.RelativeColumn(2)
                                            columns.RelativeColumn(2)
                                            columns.RelativeColumn(2)
                                            columns.RelativeColumn()
                                            columns.RelativeColumn()
                                        End Sub)

                                    table.Header(
                                        Sub(header)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Receipt").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Date").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Customer").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Payment").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Total").SemiBold().FontSize(10)
                                        End Sub)

                                    For Each s In saleList
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.ReceiptNumber).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.SaleDate.ToString("g")).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.CustomerName).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.PaymentMethod).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{s.Total:N2}").FontSize(10)
                                    Next
                                End Sub)

                            page.Footer().AlignCenter().Text(
                                Sub(text)
                                    text.Span("Page ").FontSize(9)
                                    text.CurrentPageNumber().FontSize(9)
                                    text.Span(" of ").FontSize(9)
                                    text.TotalPages().FontSize(9)
                                End Sub)
                        End Sub)
                End Sub).GeneratePdf(dlg.FileName)

            AppDialogService.ShowInfo("Report saved as PDF.", "Export")
            Return True
        End Function

        Public Shared Function ExportInventoryPdf(products As IEnumerable(Of ProductItem), title As String) As Boolean
            Dim dlg As New SaveFileDialog With {
                .Filter = "PDF files|*.pdf",
                .FileName = "Inventory.pdf",
                .DefaultExt = "pdf",
                .AddExtension = True
            }
            If dlg.ShowDialog() <> True Then Return False

            Dim productList = products.ToList()

            Document.Create(
                Sub(container)
                    container.Page(
                        Sub(page)
                            page.Margin(40)
                            page.Size(PageSizes.A4)

                            page.Header().Column(
                                Sub(col)
                                    col.Item().Text(title).FontSize(18).SemiBold()
                                    col.Item().PaddingTop(4).Text($"Generated: {DateTime.Now:g}").FontSize(9).FontColor(Colors.Grey.Darken1)
                                    col.Item().Text($"Items: {productList.Count}").FontSize(10)
                                End Sub)

                            page.Content().PaddingTop(16).Table(
                                Sub(table)
                                    table.ColumnsDefinition(
                                        Sub(columns)
                                            columns.RelativeColumn()
                                            columns.RelativeColumn(2)
                                            columns.RelativeColumn()
                                            columns.RelativeColumn()
                                            columns.RelativeColumn()
                                            columns.RelativeColumn()
                                        End Sub)

                                    table.Header(
                                        Sub(header)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("SKU").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Name").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Brand").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Price").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Qty").SemiBold().FontSize(10)
                                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Reorder").SemiBold().FontSize(10)
                                        End Sub)

                                    For Each p In productList
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Sku).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Name).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.Brand).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{p.Price:N2}").FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(p.StockOnHand.ToString()).FontSize(10)
                                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(p.ReorderLevel.ToString()).FontSize(10)
                                    Next
                                End Sub)

                            page.Footer().AlignCenter().Text(
                                Sub(text)
                                    text.Span("Page ").FontSize(9)
                                    text.CurrentPageNumber().FontSize(9)
                                    text.Span(" of ").FontSize(9)
                                    text.TotalPages().FontSize(9)
                                End Sub)
                        End Sub)
                End Sub).GeneratePdf(dlg.FileName)

            AppDialogService.ShowInfo("Inventory saved as PDF.", "Export")
            Return True
        End Function
    End Class

    Public Class InventoryService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _images As CatalogImageService = CatalogImageService.Instance

        Public Function SaveProduct(product As ProductItem, isNew As Boolean, userName As String) As ProductItem
            RequireAdmin()
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
                existing.ReorderLevel = product.ReorderLevel
                existing.StockOnHand = product.StockOnHand
                existing.ImagePath = If(product.ImagePath, String.Empty)
                If delta <> 0 Then _store.LogMovement(product.Sku, delta, "Adjustment", userName, "Manual edit")
            End If
            _store.PersistCatalog()
            Return product
        End Function

        Public Sub UpdateStockInline(sku As String, newQty As Integer, userName As String)
            RequireAdmin()
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            Dim delta = newQty - product.StockOnHand
            product.StockOnHand = newQty
            _store.LogMovement(sku, delta, "Adjustment", userName, "Inline qty edit")
            _store.PersistCatalog()
        End Sub

        Public Sub StockIn(sku As String, quantity As Integer, userName As String, notes As String)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Stock in quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            product.StockOnHand += quantity
            _store.LogMovement(sku, quantity, "Stock In", userName, If(notes, String.Empty))
            _store.PersistCatalog()
        End Sub

        Public Sub StockOut(sku As String, quantity As Integer, userName As String, notes As String)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Stock out quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            If quantity > product.StockOnHand Then
                Throw New InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.StockOnHand}")
            End If
            product.StockOnHand -= quantity
            _store.LogMovement(sku, -quantity, "Stock Out", userName, If(notes, String.Empty))
            _store.PersistCatalog()
        End Sub

        Public Sub DeleteProduct(product As ProductItem)
            RequireAdmin()
            If product Is Nothing Then Throw New ArgumentNullException(NameOf(product))
            _images.DeleteImage(product.ImagePath)
            _store.Products.Remove(product)
            _store.PersistCatalog()
        End Sub

        Private Shared Sub RequireAdmin()
            If Not SessionContext.IsAdmin Then
                Throw New UnauthorizedAccessException("Only Admin can manage inventory.")
            End If
        End Sub
    End Class
End Namespace
