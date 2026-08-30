Imports System.Windows

Namespace Models
    ''' <summary>Print dimensions derived from the selected printer and paper size.</summary>
    Public Class ReceiptLayout
        Public Property PaperWidthMm As Double = 210
        Public Property PageWidth As Double = 794
        Public Property PagePadding As Thickness = New Thickness(24)
        Public Property FontSize As Double = 11
        Public Property TitleFontSize As Double = 16
        Public Property TotalFontSize As Double = 13
        Public Property DetailFontSize As Double = 10
        Public Property FooterFontSize As Double = 12
        Public Property SeparatorLength As Integer = 42
        Public Property CharWidth As Integer = 42
        Public Property LineMargin As Double = 2
        Public Property IsReceiptPaper As Boolean

        Public Shared Function FromSettings(appSettings As AppSettings) As ReceiptLayout
            Return FromPrinterName(appSettings.ThermalPrinterName)
        End Function

        Public Shared Function FromPrinterName(printerName As String) As ReceiptLayout
            Dim layout As New ReceiptLayout()
            Dim paperWidthMm = InferPaperWidthMm(printerName, Nothing)

            layout.PaperWidthMm = paperWidthMm
            layout.IsReceiptPaper = paperWidthMm <= 85

            If layout.IsReceiptPaper Then
                layout.PageWidth = MmToWpfUnits(paperWidthMm)
                layout.PagePadding = New Thickness(4, 6, 4, 6)
                layout.FontSize = 9
                layout.TitleFontSize = 11
                layout.TotalFontSize = 11
                layout.DetailFontSize = 8.5
                layout.FooterFontSize = 10
                layout.SeparatorLength = InferThermalCharWidth(printerName, paperWidthMm)
                layout.CharWidth = layout.SeparatorLength
                layout.LineMargin = 1
            Else
                layout.PageWidth = 794
                layout.CharWidth = layout.SeparatorLength
            End If

            Return layout
        End Function

        Public Shared Function FromPrintDialog(dlg As PrintDialog) As ReceiptLayout
            Dim layout As New ReceiptLayout()
            Dim printerName = If(dlg.PrintQueue?.FullName, String.Empty)
            Dim paperWidthMm = InferPaperWidthMm(printerName, dlg)

            layout.PaperWidthMm = paperWidthMm
            layout.IsReceiptPaper = paperWidthMm <= 85

            If layout.IsReceiptPaper Then
                layout.PageWidth = MmToWpfUnits(paperWidthMm)
                layout.PagePadding = New Thickness(4, 6, 4, 6)
                layout.FontSize = 9
                layout.TitleFontSize = 11
                layout.TotalFontSize = 11
                layout.DetailFontSize = 8.5
                layout.FooterFontSize = 10
                layout.SeparatorLength = InferThermalCharWidth(printerName, paperWidthMm)
                layout.CharWidth = layout.SeparatorLength
                layout.LineMargin = 1
            Else
                layout.PageWidth = If(dlg.PrintableAreaWidth > 0, dlg.PrintableAreaWidth, 794)
                layout.CharWidth = layout.SeparatorLength
            End If

            Return layout
        End Function

        Public Shared Function InferPaperWidthMm(printerName As String, dlg As PrintDialog) As Double
            Dim name = If(printerName, String.Empty).ToUpperInvariant()

            If name.Contains("POS58") OrElse name.Contains("58MM") Then Return 58
            If name.Contains("POS80") OrElse name.Contains("80MM") Then Return 80

            Dim ticketWidth = dlg?.PrintTicket?.PageMediaSize?.Width
            If ticketWidth.HasValue AndAlso ticketWidth.Value > 0 Then
                Dim mm = ticketWidth.Value / 96.0 * 25.4
                If mm >= 45 AndAlso mm <= 85 Then Return mm
            End If

            If name.Contains("58") AndAlso (name.Contains("POS") OrElse name.Contains("THERMAL") OrElse name.Contains("RECEIPT")) Then
                Return 58
            End If
            If name.Contains("80") AndAlso (name.Contains("POS") OrElse name.Contains("THERMAL") OrElse name.Contains("RECEIPT")) Then
                Return 80
            End If
            If name.Contains("THERMAL") OrElse name.Contains("RECEIPT") Then Return 58

            Return 210
        End Function

        Public Shared Function InferThermalCharWidth(printerName As String, Optional paperWidthMm As Double = 0) As Integer
            If paperWidthMm <= 0 AndAlso Not String.IsNullOrWhiteSpace(printerName) Then
                paperWidthMm = InferPaperWidthMm(printerName, Nothing)
            End If
            If paperWidthMm > 0 AndAlso paperWidthMm <= 60 Then Return 32
            If paperWidthMm >= 70 Then Return 48
            Return 32
        End Function

        Public Shared Function MmToWpfUnits(mm As Double) As Double
            Return mm / 25.4 * 96
        End Function
    End Class
End Namespace
