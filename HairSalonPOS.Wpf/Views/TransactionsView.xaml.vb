Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class TransactionsView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub TransactionsGrid_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
            If TransactionsGrid.SelectedItem Is Nothing Then Return
            Dim vm = TryCast(DataContext, TransactionsViewModel)
            vm?.PreviewReceipt(CType(TransactionsGrid.SelectedItem, SaleRecord))
        End Sub
    End Class
End Namespace
