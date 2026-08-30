Imports System.Windows
Imports System.Windows.Documents
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class ReceiptPrintService
        Private ReadOnly _settings As AppSettingsService = AppSettingsService.Instance

        Public Sub PrintReceipt(receipt As ReceiptModel, Optional showDialog As Boolean = True)
            If receipt Is Nothing Then Throw New ArgumentNullException(NameOf(receipt))

            Dim appSettings = _settings.Settings
            If String.Equals(appSettings.PrinterType, "Thermal", StringComparison.OrdinalIgnoreCase) Then
                PrintThermalReceipt(receipt, appSettings, showDialog)
            Else
                PrintFlowDocumentReceipt(receipt, appSettings, showDialog)
            End If
        End Sub

        Private Sub PrintFlowDocumentReceipt(receipt As ReceiptModel, appSettings As AppSettings, showDialog As Boolean)
            Dim dlg As New PrintDialog()
            If showDialog AndAlso dlg.ShowDialog() <> True Then Return

            Dim layout = ReceiptLayout.FromPrintDialog(dlg)
            Dim doc = BuildFlowDocument(receipt, appSettings, layout)
            Dim pageSize = PrepareDocumentForPrint(doc, layout)

            Dim paginator = CType(doc, IDocumentPaginatorSource).DocumentPaginator
            paginator.PageSize = pageSize
            dlg.PrintDocument(paginator, receipt.ReceiptNumber)
        End Sub

        Private Sub PrintThermalReceipt(receipt As ReceiptModel, appSettings As AppSettings, showDialog As Boolean)
            Dim printerName = appSettings.ThermalPrinterName
            If showDialog OrElse String.IsNullOrWhiteSpace(printerName) Then
                Dim dlg As New PrintDialog()
                If dlg.ShowDialog() <> True Then Return
                printerName = dlg.PrintQueue.FullName
            End If

            Dim lines = BuildThermalLines(receipt, appSettings, printerName)
            Dim bytes = RawPrinterHelper.BuildEscPosReceipt(lines)
            RawPrinterHelper.SendBytesToPrinter(printerName, bytes)
        End Sub

        Public Shared Function BuildFlowDocument(receipt As ReceiptModel, appSettings As AppSettings, Optional layout As ReceiptLayout = Nothing) As FlowDocument
            If layout Is Nothing Then layout = ReceiptLayout.FromPrintDialog(New PrintDialog())

            Dim doc As New FlowDocument With {
                .FontFamily = New FontFamily("Consolas"),
                .FontSize = layout.FontSize,
                .PagePadding = layout.PagePadding,
                .TextAlignment = TextAlignment.Left,
                .Foreground = ResolveReceiptForeground(),
                .PageWidth = layout.PageWidth,
                .ColumnWidth = layout.PageWidth
            }

            AddCenter(doc, appSettings.SalonName, layout.TitleFontSize, True, layout.LineMargin)
            AddCenter(doc, appSettings.SalonAddress, layout.FontSize, False, layout.LineMargin)
            AddCenter(doc, $"Tel: {appSettings.SalonTelephone}", layout.FontSize, False, layout.LineMargin)
            AddCenter(doc, $"TIN: {appSettings.SalonTin}", layout.FontSize, False, layout.LineMargin)
            AddSeparator(doc, layout)

            AddLine(doc, $"OR No.: {receipt.ReceiptNumber}", layout.FontSize, False, layout.LineMargin)
            AddLine(doc, $"Date: {receipt.SaleDate:yyyy-MM-dd  hh:mm tt}", layout.FontSize, False, layout.LineMargin)
            AddLine(doc, $"Cashier: {receipt.CashierName}", layout.FontSize, False, layout.LineMargin)
            If Not String.IsNullOrWhiteSpace(receipt.StylistName) Then AddLine(doc, $"Stylist: {receipt.StylistName}", layout.FontSize, False, layout.LineMargin)
            AddLine(doc, $"Customer: {receipt.DisplayCustomerName}", layout.FontSize, False, layout.LineMargin)
            AddSeparator(doc, layout)

            For Each line In receipt.AllLines
                AddLine(doc, line.Name, layout.FontSize, False, layout.LineMargin)
                AddLine(doc, $"{line.Quantity} x {line.UnitPrice:N2} = {line.LineTotal:N2}", layout.DetailFontSize, False, layout.LineMargin)
            Next

            AddSeparator(doc, layout)
            AddLine(doc, $"Subtotal: {receipt.SubTotal:N2}", layout.FontSize, False, layout.LineMargin)
            If receipt.DiscountAmount > 0 Then
                AddLine(doc, $"Discount ({receipt.DiscountLabel}): -{receipt.DiscountAmount:N2}", layout.FontSize, False, layout.LineMargin)
            Else
                AddLine(doc, "Discount: 0.00", layout.FontSize, False, layout.LineMargin)
            End If
            AddLine(doc, $"TOTAL: {receipt.Total:N2}", layout.TotalFontSize, True, layout.LineMargin)
            AddSeparator(doc, layout)
            AddLine(doc, $"Payment: {receipt.PaymentMethod}", layout.FontSize, False, layout.LineMargin)
            If receipt.PaymentMethod = "Cash" Then
                AddLine(doc, $"Amount tendered: {receipt.AmountTendered:N2}", layout.FontSize, False, layout.LineMargin)
                AddLine(doc, $"Change due: {receipt.ChangeGiven:N2}", layout.FontSize, False, layout.LineMargin)
            End If
            AddSeparator(doc, layout)
            AddCenter(doc, $"Thank you for visiting {appSettings.SalonName}!", layout.FooterFontSize, False, layout.LineMargin)
            AddCenter(doc, "CUSTOMER COPY", layout.FontSize, True, layout.LineMargin)

            Return doc
        End Function

        Public Shared Function BuildThermalLines(receipt As ReceiptModel, appSettings As AppSettings, Optional printerName As String = Nothing) As List(Of String)
            Dim width = ReceiptLayout.InferThermalCharWidth(printerName)
            Dim lines As New List(Of String) From {
                "[[C]]" & appSettings.SalonName.ToUpper(),
                "[[C]]" & appSettings.SalonAddress,
                "[[C]]Tel: " & appSettings.SalonTelephone,
                "[[C]]TIN: " & appSettings.SalonTin,
                New String("-"c, width),
                $"OR No.: {receipt.ReceiptNumber}",
                $"Date: {receipt.SaleDate:yyyy-MM-dd HH:mm}",
                $"Cashier: {Truncate(receipt.CashierName, width - 9)}",
                If(String.IsNullOrWhiteSpace(receipt.StylistName), Nothing, $"Stylist: {Truncate(receipt.StylistName, width - 9)}"),
                $"Customer: {Truncate(receipt.DisplayCustomerName, width - 10)}",
                New String("-"c, width)
            }
            lines.RemoveAll(Function(s) s Is Nothing)

            For Each item In receipt.AllLines
                lines.Add(Truncate(item.Name, width))
                lines.Add($"  {item.Quantity} x {item.UnitPrice:N2} = {item.LineTotal:N2}")
            Next

            lines.Add(New String("-"c, width))
            lines.Add($"Subtotal: {receipt.SubTotal,9:N2}")
            If receipt.DiscountAmount > 0 Then
                lines.Add($"Disc ({receipt.DiscountLabel}): -{receipt.DiscountAmount:N2}")
            Else
                lines.Add("Discount: 0.00")
            End If
            lines.Add($"TOTAL: {receipt.Total,12:N2}")
            lines.Add(New String("-"c, width))
            lines.Add($"Payment: {receipt.PaymentMethod}")
            If receipt.PaymentMethod = "Cash" Then
                lines.Add($"Tendered: {receipt.AmountTendered,9:N2}")
                lines.Add($"Change: {receipt.ChangeGiven,11:N2}")
            End If
            lines.Add(New String("-"c, width))
            lines.Add("[[C]]Thank you!")
            Return lines
        End Function

        Private Shared Function PrepareDocumentForPrint(doc As FlowDocument, layout As ReceiptLayout) As Size
            doc.PageWidth = layout.PageWidth
            doc.ColumnWidth = layout.PageWidth
            doc.PagePadding = layout.PagePadding

            If layout.IsReceiptPaper Then
                doc.PageHeight = 10000
                Dim paginator = CType(doc, IDocumentPaginatorSource).DocumentPaginator
                paginator.PageSize = New Size(layout.PageWidth, 10000)

                Dim contentHeight = 800.0
                If paginator.PageCount > 0 Then
                    Dim page = paginator.GetPage(0)
                    contentHeight = page.ContentBox.Height + layout.PagePadding.Top + layout.PagePadding.Bottom + 12
                End If

                doc.PageHeight = contentHeight
                Return New Size(layout.PageWidth, contentHeight)
            End If

            Dim fallbackHeight = 1122.0
            doc.PageHeight = fallbackHeight
            Return New Size(layout.PageWidth, fallbackHeight)
        End Function

        Private Shared Function ResolveReceiptForeground() As Brush
            Dim themed = TryCast(Application.Current?.Resources("ReceiptPaperForegroundBrush"), Brush)
            If themed IsNot Nothing Then Return themed
            Return New SolidColorBrush(Color.FromRgb(&H3D, &H2B, &H1F))
        End Function

        Private Shared Sub AddCenter(doc As FlowDocument, text As String, fontSize As Double, isBold As Boolean, lineMargin As Double)
            doc.Blocks.Add(New Paragraph(New Run(text)) With {
                .TextAlignment = TextAlignment.Center,
                .FontSize = fontSize,
                .FontWeight = If(isBold, FontWeights.Bold, FontWeights.Normal),
                .Margin = New Thickness(0, 0, 0, lineMargin)
            })
        End Sub

        Private Shared Sub AddLine(doc As FlowDocument, text As String, fontSize As Double, isBold As Boolean, lineMargin As Double)
            doc.Blocks.Add(New Paragraph(New Run(text)) With {
                .FontSize = fontSize,
                .FontWeight = If(isBold, FontWeights.Bold, FontWeights.Normal),
                .Margin = New Thickness(0, 0, 0, lineMargin)
            })
        End Sub

        Private Shared Sub AddSeparator(doc As FlowDocument, layout As ReceiptLayout)
            AddLine(doc, New String("-"c, layout.SeparatorLength), layout.FontSize, False, layout.LineMargin)
        End Sub

        Private Shared Function Truncate(value As String, maxLen As Integer) As String
            If String.IsNullOrEmpty(value) Then Return String.Empty
            Return If(value.Length <= maxLen, value, value.Substring(0, maxLen))
        End Function
    End Class
End Namespace
