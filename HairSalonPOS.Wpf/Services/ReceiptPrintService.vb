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
            Dim doc = BuildFlowDocument(receipt, appSettings)
            Dim dlg As New PrintDialog()
            If showDialog AndAlso dlg.ShowDialog() <> True Then Return

            doc.PageHeight = If(dlg.PrintableAreaHeight > 0, dlg.PrintableAreaHeight, 1122)
            doc.PageWidth = If(dlg.PrintableAreaWidth > 0, dlg.PrintableAreaWidth, 794)
            doc.ColumnWidth = doc.PageWidth

            Dim paginator = CType(doc, IDocumentPaginatorSource).DocumentPaginator
            paginator.PageSize = New Size(doc.PageWidth, doc.PageHeight)
            dlg.PrintDocument(paginator, receipt.ReceiptNumber)
        End Sub

        Private Sub PrintThermalReceipt(receipt As ReceiptModel, appSettings As AppSettings, showDialog As Boolean)
            Dim printerName = appSettings.ThermalPrinterName
            If showDialog OrElse String.IsNullOrWhiteSpace(printerName) Then
                Dim dlg As New PrintDialog()
                If dlg.ShowDialog() <> True Then Return
                printerName = dlg.PrintQueue.FullName
            End If

            Dim lines = BuildThermalLines(receipt, appSettings)
            Dim bytes = RawPrinterHelper.BuildEscPosReceipt(lines)
            RawPrinterHelper.SendBytesToPrinter(printerName, bytes)
        End Sub

        Public Shared Function BuildFlowDocument(receipt As ReceiptModel, appSettings As AppSettings) As FlowDocument
            Dim doc As New FlowDocument With {
                .FontFamily = New FontFamily("Consolas"),
                .FontSize = 11,
                .PagePadding = New Thickness(24),
                .TextAlignment = TextAlignment.Left
            }

            AddCenter(doc, appSettings.SalonName, 16, True)
            AddCenter(doc, appSettings.SalonAddress)
            AddCenter(doc, $"Tel: {appSettings.SalonTelephone}")
            AddCenter(doc, $"TIN: {appSettings.SalonTin}")
            AddSeparator(doc)

            AddLine(doc, $"OR No.: {receipt.ReceiptNumber}")
            AddLine(doc, $"Date: {receipt.SaleDate:yyyy-MM-dd  hh:mm tt}")
            AddLine(doc, $"Cashier: {receipt.CashierName}")
            If Not String.IsNullOrWhiteSpace(receipt.StylistName) Then AddLine(doc, $"Stylist: {receipt.StylistName}")
            AddLine(doc, $"Customer: {receipt.DisplayCustomerName}")
            AddSeparator(doc)

            For Each line In receipt.AllLines
                AddLine(doc, line.Name)
                AddLine(doc, $"    {line.Quantity} x {line.UnitPrice:N2} = {line.LineTotal:N2}", 10)
            Next

            AddSeparator(doc)
            AddLine(doc, $"Subtotal: {receipt.SubTotal:N2}")
            If receipt.DiscountAmount > 0 Then
                AddLine(doc, $"Discount ({receipt.DiscountLabel}): -{receipt.DiscountAmount:N2}")
            Else
                AddLine(doc, "Discount: 0.00")
            End If
            AddLine(doc, $"VATable sales: {receipt.VatableSales:N2}")
            AddLine(doc, $"VAT (12%): {receipt.Tax:N2}")
            AddLine(doc, $"TOTAL: {receipt.Total:N2}", 13, True)
            AddSeparator(doc)
            AddLine(doc, $"Payment: {receipt.PaymentMethod}")
            If receipt.PaymentMethod = "Cash" Then
                AddLine(doc, $"Amount tendered: {receipt.AmountTendered:N2}")
                AddLine(doc, $"Change due: {receipt.ChangeGiven:N2}")
            End If
            AddSeparator(doc)
            AddCenter(doc, $"Thank you for visiting {appSettings.SalonName}!", 12)
            AddCenter(doc, "CUSTOMER COPY", 11, True)

            Return doc
        End Function

        Public Shared Function BuildThermalLines(receipt As ReceiptModel, appSettings As AppSettings) As List(Of String)
            Dim width = 32
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
            lines.Add($"VATable: {receipt.VatableSales,11:N2}")
            lines.Add($"VAT 12%: {receipt.Tax,9:N2}")
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

        Private Shared Sub AddCenter(doc As FlowDocument, text As String, Optional fontSize As Double = 11, Optional isBold As Boolean = False)
            doc.Blocks.Add(New Paragraph(New Run(text)) With {
                .TextAlignment = TextAlignment.Center,
                .FontSize = fontSize,
                .FontWeight = If(isBold, FontWeights.Bold, FontWeights.Normal),
                .Margin = New Thickness(0, 0, 0, 2)
            })
        End Sub

        Private Shared Sub AddLine(doc As FlowDocument, text As String, Optional fontSize As Double = 11, Optional isBold As Boolean = False)
            doc.Blocks.Add(New Paragraph(New Run(text)) With {
                .FontSize = fontSize,
                .FontWeight = If(isBold, FontWeights.Bold, FontWeights.Normal),
                .Margin = New Thickness(0, 0, 0, 2)
            })
        End Sub

        Private Shared Sub AddSeparator(doc As FlowDocument)
            AddLine(doc, New String("-"c, 42))
        End Sub

        Private Shared Function Truncate(value As String, maxLen As Integer) As String
            If String.IsNullOrEmpty(value) Then Return String.Empty
            Return If(value.Length <= maxLen, value, value.Substring(0, maxLen))
        End Function
    End Class
End Namespace
