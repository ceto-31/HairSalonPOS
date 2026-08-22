Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class InventoryProductWindow
        Inherits Window

        Private ReadOnly _viewModel As InventoryViewModel

        Public Sub New(viewModel As InventoryViewModel)
            InitializeComponent()
            _viewModel = viewModel
            DataContext = viewModel
            BindMovements()
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Escape Then
                CloseDialog()
                e.Handled = True
            End If
        End Sub

        Private Sub OverlayScrim_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.OriginalSource Is OverlayScrim Then
                CloseDialog()
                e.Handled = True
            End If
        End Sub

        Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
            CloseDialog()
        End Sub

        Private Sub StockIn_Click(sender As Object, e As RoutedEventArgs)
            e.Handled = True
            If _viewModel.RunStockInFromPopup() Then
                RefreshAfterStockChange()
            End If
        End Sub

        Private Sub StockOut_Click(sender As Object, e As RoutedEventArgs)
            e.Handled = True
            If _viewModel.RunStockOutFromPopup() Then
                RefreshAfterStockChange()
            End If
        End Sub

        Private Sub CreateOrder_Click(sender As Object, e As RoutedEventArgs)
            e.Handled = True
            If _viewModel.RunCreateOrderFromPopup() Then
                RefreshAfterStockChange()
            End If
        End Sub

        Private Sub Edit_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
            _viewModel.BeginEditFromPopup()
        End Sub

        Private Sub Delete_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
            _viewModel.DeleteFromPopup()
        End Sub

        Private Sub RefreshAfterStockChange()
            _viewModel.RefreshSelectedProductFromStore()
            BindMovements()
            UpdateCreateOrderVisibility()
        End Sub

        Private Sub BindMovements()
            MovementsList.ItemsSource = _viewModel.ProductMovements
            NoMovementsText.Visibility = If(_viewModel.HasProductMovements, Visibility.Collapsed, Visibility.Visible)
        End Sub

        Private Sub UpdateCreateOrderVisibility()
            Dim product = _viewModel.SelectedProduct
            CreateOrderButton.Visibility = If(product IsNot Nothing AndAlso product.ShowStockWarning,
                                              Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub CloseDialog()
            DialogResult = True
            Close()
        End Sub
    End Class
End Namespace
