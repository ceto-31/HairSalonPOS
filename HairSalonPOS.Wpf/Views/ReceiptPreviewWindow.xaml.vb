Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class ReceiptPreviewWindow
        Inherits Window

        Private ReadOnly _receipt As ReceiptModel
        Private ReadOnly _print As New ReceiptPrintService()
        Private ReadOnly _settings As AppSettings = AppSettingsService.Instance.Settings

        Public Sub New(receipt As ReceiptModel)
            InitializeComponent()
            _receipt = receipt
            Title = $"Receipt {receipt.ReceiptNumber}"
            TitleText.Text = $"Receipt {receipt.ReceiptNumber}"
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            RenderReceiptPreview()
        End Sub

        Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
            If Not IsLoaded Then Return
            RenderReceiptPreview()
        End Sub

        Private Sub RenderReceiptPreview()
            If _receipt Is Nothing Then Return

            Dim availableWidth = Math.Max(ActualWidth - 64, 280)
            Dim layout = ReceiptLayout.ForPreview(availableWidth)
            Dim doc = ReceiptPrintService.BuildFlowDocument(_receipt, _settings, layout)

            ReceiptViewer.Document = doc
            ReceiptViewer.Width = layout.PageWidth
            ReceiptPaperBorder.Width = layout.PageWidth
        End Sub

        Private Sub PrintButton_Click(sender As Object, e As RoutedEventArgs)
            Try
                _print.PrintReceipt(_receipt, showDialog:=True)
            Catch ex As Exception
                AppDialogService.Show(ex.Message, "Print failed", AppDialogButtons.Ok, AppDialogType.Warning, Me)
            End Try
        End Sub
    End Class
End Namespace
