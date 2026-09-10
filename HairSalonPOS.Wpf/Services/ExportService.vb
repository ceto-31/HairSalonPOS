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

        Public Shared Function ExportSalesReportPdf(data As SalesReportPdfData) As Boolean
            If data Is Nothing Then Return False

            Dim dlg As New SaveFileDialog With {
                .Filter = "PDF files|*.pdf",
                .FileName = "SalesReport.pdf",
                .DefaultExt = "pdf",
                .AddExtension = True
            }
            If dlg.ShowDialog() <> True Then Return False

            Dim saleList = If(data.Sales, Enumerable.Empty(Of SaleRecord)()).ToList()
            Dim lines = If(data.SummaryLines, Enumerable.Empty(Of String)()).ToList()
            Dim revenueBars = If(data.RevenueBars, Enumerable.Empty(Of RevenueBarItem)()).ToList()
            Dim stylists = If(data.StylistPerformance, Enumerable.Empty(Of StylistPerformanceItem)()).ToList()
            Dim accent = Colors.Brown.Darken2
            Dim accentSoft = Colors.Brown.Lighten3

            Document.Create(
                Sub(container)
                    container.Page(
                        Sub(page)
                            page.Margin(36)
                            page.Size(PageSizes.A4)

                            page.Header().Column(
                                Sub(col)
                                    col.Item().Text(If(data.Title, "Sales Report")).FontSize(18).SemiBold()
                                    col.Item().PaddingTop(4).Text($"Generated: {DateTime.Now:g}").FontSize(9).FontColor(Colors.Grey.Darken1)
                                    For Each line In lines
                                        col.Item().Text(line).FontSize(10)
                                    Next
                                End Sub)

                            page.Content().PaddingTop(12).Column(
                                Sub(col)
                                    col.Spacing(14)
                                    RenderPdfChart(col, data.DailyChart, accent, accentSoft)
                                    RenderPdfChart(col, data.WeeklyChart, accent, accentSoft)
                                    RenderPdfChart(col, data.YearlyChart, accent, accentSoft)

                                    col.Item().PaddingTop(4).Row(
                                        Sub(row)
                                            row.RelativeItem().Column(
                                                Sub(left)
                                                    left.Item().Text("Revenue by service").SemiBold().FontSize(12)
                                                    RenderPdfRevenueBars(left, revenueBars, accent)
                                                End Sub)
                                            row.ConstantItem(16)
                                            row.RelativeItem().Column(
                                                Sub(right)
                                                    right.Item().Text("Stylist performance").SemiBold().FontSize(12)
                                                    RenderPdfStylistTable(right, stylists)
                                                End Sub)
                                        End Sub)

                                    col.Item().PageBreak()
                                    col.Item().Text("Transaction list").SemiBold().FontSize(12)
                                    col.Item().PaddingTop(6).Table(
                                        Sub(table)
                                            table.ColumnsDefinition(
                                                Sub(columns)
                                                    columns.RelativeColumn()
                                                    columns.RelativeColumn()
                                                    columns.RelativeColumn(1.4F)
                                                    columns.RelativeColumn()
                                                    columns.RelativeColumn()
                                                    columns.RelativeColumn()
                                                    columns.RelativeColumn()
                                                End Sub)

                                            table.Header(
                                                Sub(header)
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Receipt").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Date").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Customer").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Cashier").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Stylist").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Payment").SemiBold()
                                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Total").SemiBold()
                                                End Sub)

                                            For Each s In saleList
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.ReceiptNumber)
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.SaleDate.ToString("g"))
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.CustomerName)
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.CashierName)
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.StylistName)
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.PaymentMethod)
                                                table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{s.Total:N2}")
                                            Next
                                        End Sub)
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

        Private Shared Sub RenderPdfChart(col As ColumnDescriptor, chart As DashboardLineChart, accent As String, accentSoft As String)
            If chart Is Nothing OrElse chart.Points Is Nothing OrElse chart.Points.Count = 0 Then Return

            col.Item().Border(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(
                Sub(chartCol)
                    chartCol.Item().Row(
                        Sub(titleRow)
                            titleRow.RelativeItem().Text(chart.Title).SemiBold().FontSize(12)
                            titleRow.ConstantItem(80).AlignRight().Text(chart.MaxAmountLabel).FontSize(9).FontColor(Colors.Grey.Darken1)
                        End Sub)
                    chartCol.Item().Text(chart.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken1)

                    Dim points = chart.Points.ToList()
                    Dim maxAmount = points.Max(Function(p) p.Amount)
                    If maxAmount <= 0D Then maxAmount = 1D
                    Const chartHeight As Single = 90

                    chartCol.Item().PaddingTop(8).Height(chartHeight).Row(
                        Sub(row)
                            For Each pt In points
                                Dim ratio = If(maxAmount > 0D, CDbl(pt.Amount) / CDbl(maxAmount), 0R)
                                Dim barHeight = CSng(Math.Max(2, ratio * (chartHeight - 18)))
                                row.RelativeItem().PaddingHorizontal(1).Column(
                                    Sub(barCol)
                                        barCol.Item().AlignBottom().Height(barHeight).
                                            Background(If(pt.IsEmphasis, accent, accentSoft))
                                        If pt.ShowLabel Then
                                            barCol.Item().PaddingTop(3).AlignCenter().Text(pt.Label).FontSize(7)
                                        End If
                                        If pt.Amount > 0D Then
                                            barCol.Item().AlignCenter().Text($"₱{pt.Amount:N2}").FontSize(6).FontColor(Colors.Grey.Darken1)
                                        End If
                                    End Sub)
                            Next
                        End Sub)
                End Sub)
        End Sub

        Private Shared Sub RenderPdfRevenueBars(col As ColumnDescriptor, bars As IList(Of RevenueBarItem), accent As String)
            If bars Is Nothing OrElse bars.Count = 0 Then
                col.Item().PaddingTop(8).Text("No service revenue data.").FontSize(9).FontColor(Colors.Grey.Darken1)
                Return
            End If

            Dim maxAmount = bars.Max(Function(b) b.Amount)
            If maxAmount <= 0D Then maxAmount = 1D
            Const chartHeight As Single = 100

            col.Item().PaddingTop(8).Height(chartHeight).Row(
                Sub(row)
                    For Each bar In bars
                        Dim ratio = CDbl(bar.Amount) / CDbl(maxAmount)
                        Dim barHeight = CSng(Math.Max(6, ratio * (chartHeight - 24)))
                        row.RelativeItem().PaddingHorizontal(2).Column(
                            Sub(barCol)
                                barCol.Item().AlignBottom().Height(barHeight).Background(accent)
                                barCol.Item().PaddingTop(4).AlignCenter().Text(bar.Label).FontSize(7)
                                barCol.Item().AlignCenter().Text($"₱{bar.Amount:N2}").FontSize(7).FontColor(Colors.Grey.Darken1)
                            End Sub)
                    Next
                End Sub)
        End Sub

        Private Shared Sub RenderPdfStylistTable(col As ColumnDescriptor, stylists As IList(Of StylistPerformanceItem))
            col.Item().PaddingTop(8).Table(
                Sub(table)
                    table.ColumnsDefinition(
                        Sub(columns)
                            columns.RelativeColumn(1.4F)
                            columns.RelativeColumn()
                            columns.RelativeColumn()
                        End Sub)

                    table.Header(
                        Sub(header)
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Stylist").SemiBold()
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Services").SemiBold()
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Revenue").SemiBold()
                        End Sub)

                    If stylists.Count = 0 Then
                        table.Cell().ColumnSpan(3).Padding(4).Text("No stylist performance data.").FontColor(Colors.Grey.Darken1)
                        Return
                    End If

                    For Each item In stylists
                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.StylistName)
                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ServiceCount.ToString())
                        table.Cell().BorderBottom(0.5F).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{item.Revenue:N2}")
                    Next
                End Sub)
        End Sub

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
            ValidateBoxCodeForProduct(sku, boxCode, product.Name)
            Dim quantityPieces = ResolveQuantityPieces(product, quantity, boxesReceived)
            If quantityPieces <= 0 Then Throw New InvalidOperationException("Stock in quantity must be positive.")
            _store.AddStockBatch(sku, quantityPieces, False, "Stock In", expirationDate, boxCode, boxesReceived)
            ApplyProductExpiration(product, expirationDate)
            _store.LogMovement(sku, quantityPieces, "Stock In", userName, If(notes, String.Empty), expirationDate, boxCode)
            _store.PersistCatalog()
        End Sub

        Public Sub StockOut(sku As String, quantity As Integer, userName As String, notes As String,
                           Optional batchId As Integer? = Nothing)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Stock out quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            If quantity > product.StockOnHand Then
                Throw New InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.StockOnHand}")
            End If

            If batchId.HasValue Then
                Dim taken = _store.DeductFromBatch(batchId.Value, quantity)
                _store.LogMovement(sku, -taken.Taken, "Stock Out", userName, If(notes, String.Empty),
                                    taken.Batch.ExpirationDate, taken.Batch.BoxCode)
            Else
                For Each taken In _store.DeductFefo(sku, quantity, allowReserve:=False)
                    _store.LogMovement(sku, -taken.Taken, "Stock Out", userName, If(notes, String.Empty),
                                        taken.Batch.ExpirationDate, taken.Batch.BoxCode)
                Next
            End If

            _store.PersistCatalog()
        End Sub

        Public Sub StockOutFromReserve(sku As String, quantity As Integer, userName As String, notes As String, batchId As Integer)
            RequireAdmin()
            If quantity <= 0 Then Throw New InvalidOperationException("Stock out quantity must be positive.")
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            If quantity > product.ReservedQty Then
                Throw New InvalidOperationException(
                    $"Insufficient reserve stock for {product.Name}. Available: {product.ReservedQty}")
            End If

            Dim taken = _store.DeductFromBatch(batchId, quantity, allowReserve:=True)
            If Not taken.Batch.IsReserve Then
                Throw New InvalidOperationException("Selected batch is not reserve stock.")
            End If

            Dim detail = If(String.IsNullOrWhiteSpace(notes), "Reserve stock release", $"Reserve stock release — {notes.Trim()}")
            _store.LogMovement(sku, -taken.Taken, "Stock Out", userName, detail,
                                taken.Batch.ExpirationDate, taken.Batch.BoxCode)
            _store.PersistCatalog()
        End Sub

        ''' <summary>Add units to the reserve stock pool (independent from on-hand).</summary>
        Public Sub ReserveStock(sku As String, quantity As Integer, userName As String, notes As String,
                                Optional expirationDate As Date? = Nothing,
                                Optional boxCode As String = Nothing,
                                Optional boxesReceived As Integer = 0)
            RequireAdmin()
            Dim product = _store.Products.First(Function(p) p.Sku = sku)
            product.EnsureDefaults()
            ValidateBoxCodeForProduct(sku, boxCode, product.Name)
            Dim quantityPieces = ResolveQuantityPieces(product, quantity, boxesReceived)
            If quantityPieces <= 0 Then Throw New InvalidOperationException("Reserve quantity must be positive.")
            _store.AddStockBatch(sku, quantityPieces, True, "Add Reserve Stock", expirationDate, boxCode, boxesReceived)
            ApplyProductExpiration(product, expirationDate)
            Dim detail = $"Reserve stock +{quantityPieces}"
            If Not String.IsNullOrWhiteSpace(notes) Then detail &= $" — {notes.Trim()}"
            _store.LogMovement(sku, quantityPieces, "Add Reserve Stock", userName, detail, expirationDate, boxCode)
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

        Private Sub ValidateBoxCodeForProduct(sku As String, boxCode As String, productName As String)
            If String.IsNullOrWhiteSpace(boxCode) Then Return

            Dim conflictProduct = _store.GetProductUsingBoxCode(boxCode, sku)
            If conflictProduct Is Nothing Then Return

            Dim label = If(String.IsNullOrWhiteSpace(conflictProduct.Name), conflictProduct.Sku, conflictProduct.Name)
            Throw New InvalidOperationException(
                $"Box code {boxCode.Trim()} is already assigned to {label} (SKU {conflictProduct.Sku}). " &
                $"Use a different box code for {productName}.")
        End Sub

        Private Shared Sub RequireAdmin()
            If Not SessionContext.IsAdmin Then
                Throw New UnauthorizedAccessException("Only Admin can manage inventory.")
            End If
        End Sub
    End Class
End Namespace
