Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class InventoryView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ProductList_PreviewMouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            Dim vm = TryCast(DataContext, InventoryViewModel)
            If vm Is Nothing OrElse vm.IsEditMode Then Return
            If Not vm.IsStockInTab AndAlso Not vm.IsStockOutTab Then Return

            Dim item = TryCast(FindAncestor(Of ListBoxItem)(e.OriginalSource), ListBoxItem)
            If item Is Nothing OrElse item.DataContext Is Nothing Then Return

            Dim product = TryCast(item.DataContext, HairSalonPOS.Wpf.Models.ProductItem)
            If product Is Nothing Then Return

            If Object.ReferenceEquals(vm.SelectedProduct, product) Then
                vm.PromptStockForSelectedProduct()
            End If
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
