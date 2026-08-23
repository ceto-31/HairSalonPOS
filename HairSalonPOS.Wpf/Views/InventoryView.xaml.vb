Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class InventoryView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ProductList_PreviewMouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton <> MouseButton.Left Then Return

            Dim vm = GetViewModel()
            If vm Is Nothing OrElse vm.IsEditMode Then Return
            If Not vm.IsStockInTab AndAlso Not vm.IsStockOutTab Then Return

            Try
                Dim item = FindAncestor(Of ListBoxItem)(e.OriginalSource)
                If item Is Nothing Then Return

                Dim product = TryCast(item.DataContext, ProductItem)
                vm.ActivateProductForStockMovement(product)
            Catch ex As Exception
                AppDialogService.ShowError(
                    $"Something went wrong while opening stock movement.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    If(vm?.IsStockOutTab, "Stock out", "Stock in"))
            End Try
        End Sub

        Private Function GetViewModel() As InventoryViewModel
            Return TryCast(DataContext, InventoryViewModel)
        End Function

        Private Shared Function FindAncestor(Of T As DependencyObject)(source As Object) As T
            Dim current = TryCast(source, DependencyObject)
            While current IsNot Nothing
                Dim match = TryCast(current, T)
                If match IsNot Nothing Then Return match
                current = System.Windows.Media.VisualTreeHelper.GetParent(current)
            End While
            Return Nothing
        End Function
    End Class
End Namespace
