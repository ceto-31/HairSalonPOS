Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class ReceiptPreviewWindow
        Inherits Window

        Private ReadOnly _receipt As ReceiptModel
        Private ReadOnly _print As New ReceiptPrintService()

        Public Sub New(receipt As ReceiptModel)
            InitializeComponent()
            _receipt = receipt
            Title = $"Receipt {receipt.ReceiptNumber}"
            TitleText.Text = $"Receipt {receipt.ReceiptNumber}"
            Dim settings = AppSettingsService.Instance.Settings
            Dim layout = ReceiptLayout.FromSettings(settings)
            ReceiptViewer.Document = ReceiptPrintService.BuildFlowDocument(receipt, settings, layout)
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
