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

        Public Function SaveProduct(product As ProductItem, isNew As Boolean, userName As String, targetStockQty As Integer) As ProductItem
            RequireAdmin()
            If isNew Then
                If _store.Products.Any(Function(p) p.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase)) Then
                    Throw New InvalidOperationException("SKU already exists.")
                End If
                _store.Products.Add(product)
                If targetStockQty > 0 Then
                    _store.AddStockBatch(product.Sku, targetStockQty, False, "Restock")
                    _store.LogMovement(product.Sku, targetStockQty, "Restock", userName, "Initial stock")
                End If
            Else
                Dim existing = _store.Products.First(Function(p) p.Sku = product.Sku)
                Dim delta = targetStockQty - existing.StockOnHand
                existing.Name = product.Name
                existing.Brand = product.Brand
                existing.Price = product.Price
                existing.ReorderLevel = product.ReorderLevel
                existing.Category = product.Category
                existing.SubCategory = product.SubCategory
                existing.ImagePath = If(product.ImagePath, String.Empty)
                ApplyStockDelta(existing.Sku, delta, userName, "Manual edit")
            End If
            _store.PersistCatalog()
            Return product
        End Function

        Public Sub UpdateStockInline(sku As String, newQty As Integer, userName As String)
            RequireAdmin()
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            Dim delta = newQty - product.StockOnHand
            ApplyStockDelta(sku, delta, userName, "Inline qty edit")
            _store.PersistCatalog()
        End Sub

        Public Sub StockIn(sku As String, quantity As Integer, userName As String, notes As String,
                           Optional expirationDate As Date? = Nothing,
                           Optional boxCode As String = Nothing,
                           Optional boxesReceived As Integer = 0)
            RequireAdmin()
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            Dim quantityPieces = ResolveQuantityPieces(product, quantity, boxesReceived)
            If quantityPieces <= 0 Then Throw New InvalidOperationException("Stock in quantity must be positive.")
            _store.AddStockBatch(sku, quantityPieces, False, "Stock In", expirationDate, boxCode, boxesReceived)
            ApplyProductExpiration(product, expirationDate)
            _store.LogMovement(sku, quantityPieces, "Stock In", userName, If(notes, String.Empty), expirationDate, boxCode)
            _store.PersistCatalog()
        End Sub

        Public Sub StockOut(sku As String, quantity As Integer, userName As String, notes As String)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Stock out quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            If quantity > product.StockOnHand Then
                Throw New InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.StockOnHand}")
            End If
            For Each taken In _store.DeductFefo(sku, quantity, allowReserve:=False)
                _store.LogMovement(sku, -taken.Taken, "Stock Out", userName, If(notes, String.Empty),
                                    taken.Batch.ExpirationDate, taken.Batch.BoxCode)
            Next
            _store.PersistCatalog()
        End Sub

        ''' <summary>Add units to the reserve stock pool (independent from on-hand).</summary>
        Public Sub ReserveStock(sku As String, quantity As Integer, userName As String, notes As String,
                                Optional expirationDate As Date? = Nothing,
                                Optional boxesReceived As Integer = 0)
            RequireAdmin()
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            product.EnsureDefaults()
            Dim quantityPieces = ResolveQuantityPieces(product, quantity, boxesReceived)
            If quantityPieces <= 0 Then Throw New InvalidOperationException("Reserve quantity must be positive.")
            _store.AddStockBatch(sku, quantityPieces, True, "Add Reserve Stock", expirationDate, boxesReceived:=boxesReceived)
            ApplyProductExpiration(product, expirationDate)
            Dim detail = $"Reserve stock +{quantityPieces}"
            If Not String.IsNullOrWhiteSpace(notes) Then detail &= $" — {notes.Trim()}"
            _store.LogMovement(sku, quantityPieces, "Add Reserve Stock", userName, detail, expirationDate)
            _store.PersistCatalog()
        End Sub

        ''' <summary>Transfer reserve stock back to on-hand — only when on-hand is depleted.</summary>
        Public Sub ReleaseReserve(sku As String, quantity As Integer, userName As String, notes As String)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Release quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            product.EnsureDefaults()
            If product.StockOnHand > 0 Then
                Throw New InvalidOperationException(
                    $"Reserve stock can only be used when on-hand is depleted. {product.Name} still has {product.StockOnHand} on hand.")
            End If
            If quantity > product.ReservedQty Then
                Throw New InvalidOperationException(
                    $"Cannot use {quantity} from reserve stock for {product.Name}. Only {product.ReservedQty} in reserve.")
            End If

            Dim taken = _store.DeductFromPool(sku, quantity, isReserve:=True)
            If taken.Sum(Function(t) t.Taken) < quantity Then
                Throw New InvalidOperationException(
                    $"Cannot use {quantity} from reserve stock for {product.Name}. Only {product.ReservedQty} in reserve.")
            End If

            For Each item In taken
                _store.AddStockBatch(sku, item.Taken, False, "Use Reserve Stock",
                                     item.Batch.ExpirationDate, item.Batch.BoxCode)
            Next
            _store.PersistStockBatches()

            Dim detail = $"Reserve stock -{quantity} (restored to on-hand)"
            If Not String.IsNullOrWhiteSpace(notes) Then detail &= $" — {notes.Trim()}"
            _store.LogMovement(sku, quantity, "Use Reserve Stock", userName, detail)
            _store.PersistCatalog()
        End Sub

        Private Sub ApplyStockDelta(sku As String, delta As Integer, userName As String, notes As String)
            If delta = 0 Then Return
            If delta > 0 Then
                _store.AddStockBatch(sku, delta, False, "Adjustment")
                _store.LogMovement(sku, delta, "Adjustment", userName, notes)
            Else
                For Each taken In _store.DeductFefo(sku, -delta, allowReserve:=False)
                    _store.LogMovement(sku, -taken.Taken, "Adjustment", userName, notes,
                                        taken.Batch.ExpirationDate, taken.Batch.BoxCode)
                Next
            End If
        End Sub

        Public Sub DeleteProduct(product As ProductItem)
            RequireAdmin()
            If product Is Nothing Then Throw New ArgumentNullException(NameOf(product))
            _images.DeleteImage(product.ImagePath)
            _store.Products.Remove(product)
            _store.PersistCatalog()
        End Sub

        Private Shared Function ResolveQuantityPieces(product As ProductItem, quantity As Integer, boxesReceived As Integer) As Integer
            If boxesReceived > 0 Then
                product?.EnsureDefaults()
                Dim unitsPerBox = If(product Is Nothing OrElse product.UnitsPerBox <= 0, 1, product.UnitsPerBox)
                Return boxesReceived * unitsPerBox
            End If
            Return quantity
        End Function

        Private Shared Sub ApplyProductExpiration(product As ProductItem, expirationDate As Date?)
            If product Is Nothing OrElse Not expirationDate.HasValue Then Return
            If Not product.ExpirationDate.HasValue OrElse expirationDate.Value < product.ExpirationDate.Value Then
                product.ExpirationDate = expirationDate.Value
            End If
        End Sub

        Private Shared Sub RequireAdmin()
            If Not SessionContext.IsAdmin Then
                Throw New UnauthorizedAccessException("Only Admin can manage inventory.")
            End If
        End Sub
    End Class
End Namespace
