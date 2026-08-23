Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class InventoryView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ProductList_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
            Dim vm = TryCast(DataContext, InventoryViewModel)
            If vm Is Nothing OrElse vm.IsEditMode Then Return
            If Not vm.IsStockInTab AndAlso Not vm.IsStockOutTab Then Return

            Dim item = FindAncestor(Of ListBoxItem)(e.OriginalSource)
            If item Is Nothing Then Return

            Dim product = TryCast(item.DataContext, ProductItem)
            If product Is Nothing Then Return

            vm.OpenStockMovementForProduct(product)
            e.Handled = True
        End Sub

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
