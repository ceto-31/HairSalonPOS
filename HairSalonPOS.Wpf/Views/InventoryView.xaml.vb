Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media
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
            If Not vm.IsStockInTab AndAlso Not vm.IsStockOutTab AndAlso Not vm.IsReserveStockTab Then Return

            Dim item = FindAncestor(Of ListBoxItem)(e.OriginalSource)
            If item Is Nothing Then Return

            Dim product = TryCast(item.DataContext, ProductItem)
            If product Is Nothing Then Return

            vm.OpenStockMovementForProduct(product)
            e.Handled = True
        End Sub

        Private Shared Function FindAncestor(Of T As DependencyObject)(source As Object) As T
            Dim current = ResolveDependencyObject(source)
            While current IsNot Nothing
                Dim match = TryCast(current, T)
                If match IsNot Nothing Then Return match
                current = GetParentObject(current)
            End While
            Return Nothing
        End Function

        Private Shared Function ResolveDependencyObject(source As Object) As DependencyObject
            Dim dep = TryCast(source, DependencyObject)
            If dep IsNot Nothing Then Return dep

            Dim content = TryCast(source, ContentElement)
            If content IsNot Nothing Then
                Dim textElement = TryCast(content, TextElement)
                If textElement IsNot Nothing Then Return TryCast(textElement.Parent, DependencyObject)
            End If

            Return Nothing
        End Function

        Private Shared Function GetParentObject(current As DependencyObject) As DependencyObject
            If TypeOf current Is Visual OrElse TypeOf current Is Media3D.Visual3D Then
                Return TryCast(VisualTreeHelper.GetParent(current), DependencyObject)
            End If

            Return TryCast(LogicalTreeHelper.GetParent(current), DependencyObject)
        End Function
    End Class
End Namespace
