Imports System.Windows.Controls

Namespace Views
    Partial Public Class InventoryView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ProductsGrid_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs)
            If e.EditAction <> DataGridEditAction.Commit Then Return
            Dim product = TryCast(e.Row.Item, Models.ProductItem)
            If product Is Nothing Then Return
            Dim vm = TryCast(DataContext, ViewModels.InventoryViewModel)
            If vm Is Nothing Then Return
            Dim textBox = TryCast(e.EditingElement, TextBox)
            If textBox Is Nothing Then Return
            Dim newQty As Integer
            If Integer.TryParse(textBox.Text, newQty) Then
                vm.UpdateQtyInline(product, newQty)
            End If
        End Sub
    End Class
End Namespace
