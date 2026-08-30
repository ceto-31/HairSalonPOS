Imports System.Windows
Imports System.Windows.Documents
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Helpers
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
            If layout Is Nothing Then layout = ReceiptLayout.FromSettings(appSettings)

            Dim doc As New FlowDocument With {
                .FontFamily = New FontFamily("Consolas"),
                .FontSize = layout.FontSize,
                .PagePadding = layout.PagePadding,
                .TextAlignment = TextAlignment.Left,
                .Foreground = ResolveReceiptForeground(),
                .PageWidth = layout.PageWidth,
                .ColumnWidth = layout.PageWidth
            }

            Dim width = layout.CharWidth
            Dim useFixedWidth = layout.IsReceiptPaper

            If useFixedWidth Then
                AddWrappedCenter(doc, appSettings.SalonName, width, layout.TitleFontSize, True, layout.LineMargin)
                AddWrappedCenter(doc, appSettings.SalonAddress, width, layout.FontSize, False, layout.LineMargin)
                AddWrappedCenter(doc, $"Tel: {appSettings.SalonTelephone}", width, layout.FontSize, False, layout.LineMargin)
                AddWrappedCenter(doc, $"TIN: {appSettings.SalonTin}", width, layout.FontSize, False, layout.LineMargin)
            Else
                AddCenter(doc, appSettings.SalonName, layout.TitleFontSize, True, layout.LineMargin)
                AddCenter(doc, appSettings.SalonAddress, layout.FontSize, False, layout.LineMargin)
                AddCenter(doc, $"Tel: {appSettings.SalonTelephone}", layout.FontSize, False, layout.LineMargin)
                AddCenter(doc, $"TIN: {appSettings.SalonTin}", layout.FontSize, False, layout.LineMargin)
            End If
            AddSeparator(doc, layout)

            AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"OR No.: {receipt.ReceiptNumber}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"Date: {receipt.SaleDate:yyyy-MM-dd  hh:mm tt}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"Cashier: {receipt.CashierName}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            If Not String.IsNullOrWhiteSpace(receipt.StylistName) Then
                AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"Stylist: {receipt.StylistName}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            End If
            AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"Customer: {receipt.DisplayCustomerName}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            AddSeparator(doc, layout)

            For Each line In receipt.AllLines
                If useFixedWidth Then
                    AddReceiptLines(doc, ReceiptTextFormatter.WrapText(line.Name, width), layout.FontSize, False, layout.LineMargin)
                    AddLine(doc, ReceiptTextFormatter.FormatItemDetailLine(line.Quantity, line.UnitPrice, line.LineTotal, width), layout.DetailFontSize, False, layout.LineMargin)
                Else
                    AddLine(doc, line.Name, layout.FontSize, False, layout.LineMargin)
                    AddLine(doc, $"{line.Quantity} x {line.UnitPrice:N2} = {line.LineTotal:N2}", layout.DetailFontSize, False, layout.LineMargin)
                End If
            Next

            AddSeparator(doc, layout)
            If useFixedWidth Then
                AddLine(doc, ReceiptTextFormatter.FormatAmountLine("Subtotal:", receipt.SubTotal, width), layout.FontSize, False, layout.LineMargin)
                If receipt.DiscountAmount > 0 Then
                    AddLine(doc, ReceiptTextFormatter.FormatLeftRight($"Discount ({receipt.DiscountLabel}):", $"-{receipt.DiscountAmount:N2}", width), layout.FontSize, False, layout.LineMargin)
                Else
                    AddLine(doc, ReceiptTextFormatter.FormatAmountLine("Discount:", 0D, width), layout.FontSize, False, layout.LineMargin)
                End If
                AddLine(doc, ReceiptTextFormatter.FormatAmountLine("TOTAL:", receipt.Total, width), layout.TotalFontSize, True, layout.LineMargin)
            Else
                AddLine(doc, $"Subtotal: {receipt.SubTotal:N2}", layout.FontSize, False, layout.LineMargin)
                If receipt.DiscountAmount > 0 Then
                    AddLine(doc, $"Discount ({receipt.DiscountLabel}): -{receipt.DiscountAmount:N2}", layout.FontSize, False, layout.LineMargin)
                Else
                    AddLine(doc, "Discount: 0.00", layout.FontSize, False, layout.LineMargin)
                End If
                AddLine(doc, $"TOTAL: {receipt.Total:N2}", layout.TotalFontSize, True, layout.LineMargin)
            End If
            AddSeparator(doc, layout)
            AddReceiptLines(doc, ReceiptTextFormatter.WrapText($"Payment: {receipt.PaymentMethod}", If(useFixedWidth, width, Integer.MaxValue)), layout.FontSize, False, layout.LineMargin)
            If receipt.PaymentMethod = "Cash" Then
                If useFixedWidth Then
                    AddLine(doc, ReceiptTextFormatter.FormatAmountLine("Amount tendered:", receipt.AmountTendered, width), layout.FontSize, False, layout.LineMargin)
                    AddLine(doc, ReceiptTextFormatter.FormatAmountLine("Change due:", receipt.ChangeGiven, width), layout.FontSize, False, layout.LineMargin)
                Else
                    AddLine(doc, $"Amount tendered: {receipt.AmountTendered:N2}", layout.FontSize, False, layout.LineMargin)
                    AddLine(doc, $"Change due: {receipt.ChangeGiven:N2}", layout.FontSize, False, layout.LineMargin)
                End If
            End If
            AddSeparator(doc, layout)
            If useFixedWidth Then
                AddWrappedCenter(doc, $"Thank you for visiting {appSettings.SalonName}!", width, layout.FooterFontSize, False, layout.LineMargin)
            Else
                AddCenter(doc, $"Thank you for visiting {appSettings.SalonName}!", layout.FooterFontSize, False, layout.LineMargin)
            End If
            AddCenter(doc, "CUSTOMER COPY", layout.FontSize, True, layout.LineMargin)

            Return doc
        End Function

        Public Shared Function BuildThermalLines(receipt As ReceiptModel, appSettings As AppSettings, Optional printerName As String = Nothing) As List(Of String)
            Dim width = ReceiptLayout.InferThermalCharWidth(printerName)
            Dim lines As New List(Of String)

            AddCenteredWrappedLines(lines, appSettings.SalonName.ToUpper(), width)
            AddCenteredWrappedLines(lines, appSettings.SalonAddress, width)
            AddCenteredWrappedLines(lines, "Tel: " & appSettings.SalonTelephone, width)
            AddCenteredWrappedLines(lines, "TIN: " & appSettings.SalonTin, width)
            lines.Add(New String("-"c, width))

            lines.AddRange(ReceiptTextFormatter.WrapText($"OR No.: {receipt.ReceiptNumber}", width))
            lines.AddRange(ReceiptTextFormatter.WrapText($"Date: {receipt.SaleDate:yyyy-MM-dd HH:mm}", width))
            lines.AddRange(ReceiptTextFormatter.WrapText($"Cashier: {receipt.CashierName}", width))
            If Not String.IsNullOrWhiteSpace(receipt.StylistName) Then
                lines.AddRange(ReceiptTextFormatter.WrapText($"Stylist: {receipt.StylistName}", width))
            End If
            lines.AddRange(ReceiptTextFormatter.WrapText($"Customer: {receipt.DisplayCustomerName}", width))
            lines.Add(New String("-"c, width))

            For Each item In receipt.AllLines
                lines.AddRange(ReceiptTextFormatter.WrapText(item.Name, width))
                lines.Add(ReceiptTextFormatter.FormatItemDetailLine(item.Quantity, item.UnitPrice, item.LineTotal, width))
            Next

            lines.Add(New String("-"c, width))
            lines.Add(ReceiptTextFormatter.FormatAmountLine("Subtotal:", receipt.SubTotal, width))
            If receipt.DiscountAmount > 0 Then
                lines.Add(ReceiptTextFormatter.FormatLeftRight($"Disc ({receipt.DiscountLabel}):", $"-{receipt.DiscountAmount:N2}", width))
            Else
                lines.Add(ReceiptTextFormatter.FormatAmountLine("Discount:", 0D, width))
            End If
            lines.Add(ReceiptTextFormatter.FormatAmountLine("TOTAL:", receipt.Total, width))
            lines.Add(New String("-"c, width))
            lines.AddRange(ReceiptTextFormatter.WrapText($"Payment: {receipt.PaymentMethod}", width))
            If receipt.PaymentMethod = "Cash" Then
                lines.Add(ReceiptTextFormatter.FormatAmountLine("Tendered:", receipt.AmountTendered, width))
                lines.Add(ReceiptTextFormatter.FormatAmountLine("Change:", receipt.ChangeGiven, width))
            End If
            lines.Add(New String("-"c, width))
            AddCenteredWrappedLines(lines, $"Thank you for visiting {appSettings.SalonName}!", width)

            Return lines
        End Function

        Private Shared Sub AddCenteredWrappedLines(lines As List(Of String), text As String, width As Integer)
            For Each wrapped In ReceiptTextFormatter.WrapText(text, width)
                lines.Add("[[C]]" & wrapped)
            Next
        End Sub

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

        Private Shared Sub AddWrappedCenter(doc As FlowDocument, text As String, width As Integer, fontSize As Double, isBold As Boolean, lineMargin As Double)
            For Each wrapped In ReceiptTextFormatter.WrapText(text, width)
                AddCenter(doc, wrapped, fontSize, isBold, lineMargin)
            Next
        End Sub

        Private Shared Sub AddReceiptLines(doc As FlowDocument, wrappedLines As IEnumerable(Of String), fontSize As Double, isBold As Boolean, lineMargin As Double)
            For Each wrapped In wrappedLines
                AddLine(doc, wrapped, fontSize, isBold, lineMargin)
            Next
        End Sub

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
    End Class
End Namespace
