Imports System.Text.RegularExpressions
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class StockMovementWindow
        Inherits Window

        Private Shared ReadOnly DigitsOnly As New Regex("^\d+$")
        Private Shared ReadOnly StockInReasons As String() = {"Purchase", "Customer return", "Transfer in", "Other"}
        Private Shared ReadOnly StockOutReasons As String() = {"Damaged", "Expired", "Used in service", "Missing", "Return to supplier", "Other"}

        Private ReadOnly _isStockIn As Boolean
        Private ReadOnly _currentQty As Integer
        Private _quantity As Integer = 1

        Public Property Confirmed As Boolean
        Public Property ResultQuantity As Integer
        Public Property ResultReason As String = String.Empty
        Public Property ResultNotes As String = String.Empty

        Public Sub New(product As ProductItem, isStockIn As Boolean, Optional initialQty As Integer = 1)
            InitializeComponent()
            _isStockIn = isStockIn
            _currentQty = If(product Is Nothing, 0, product.StockOnHand)

            TitleText.Text = If(isStockIn, "Stock in", "Stock out")
            ConfirmButton.Content = If(isStockIn, "Stock in", "Stock out")
            ProductNameText.Text = If(product?.Name, "Product")
            ProductMetaText.Text = $"SKU {If(product?.Sku, "—")}  •  On hand {_currentQty}"

            If isStockIn Then
                AccentBar.Background = TryCast(FindResource("LinkStockInBrush"), Brush)
                ReasonBox.ItemsSource = StockInReasons
                If ReasonBox.Items.Count > 0 Then ReasonBox.SelectedIndex = 0
            Else
                AccentBar.Background = TryCast(FindResource("LinkDeleteBrush"), Brush)
                ReasonBox.ItemsSource = StockOutReasons
                If ReasonBox.Items.Count > 0 Then ReasonBox.SelectedIndex = 0
            End If

            Dim photo = CatalogImageService.Instance.CreateImageSource(product?.ImagePath)
            If photo IsNot Nothing Then
                ProductPhoto.Source = photo
                ProductPhoto.Visibility = Visibility.Visible
                PhotoPlaceholder.Visibility = Visibility.Collapsed
            Else
                PhotoPlaceholder.Text = If(product?.PlaceholderIcon, "📦")
                PhotoPlaceholder.FontSize = 28
            End If

            SetQuantity(Math.Max(1, initialQty))
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
            Dispatcher.BeginInvoke(Sub()
                                       QtyBox.Focus()
                                       QtyBox.SelectAll()
                                   End Sub)
        End Sub

        Private Sub OverlayScrim_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.OriginalSource Is OverlayScrim Then
                CancelSelection()
                e.Handled = True
            End If
        End Sub

        Private Sub DialogContent_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            e.Handled = False
        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Escape Then
                CancelSelection()
                e.Handled = True
            End If
        End Sub

        Private Sub Minus_Click(sender As Object, e As RoutedEventArgs)
            SetQuantity(Math.Max(1, _quantity - 1))
        End Sub

        Private Sub Plus_Click(sender As Object, e As RoutedEventArgs)
            SetQuantity(_quantity + 1)
        End Sub

        Private Sub QtyBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            e.Handled = Not DigitsOnly.IsMatch(e.Text)
        End Sub

        Private Sub QtyBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            Dim parsed As Integer
            If Integer.TryParse(QtyBox.Text, parsed) Then
                _quantity = Math.Max(1, parsed)
                UpdatePreview()
            End If
        End Sub

        Private Sub Confirm_Click(sender As Object, e As RoutedEventArgs)
            ConfirmSelection()
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            CancelSelection()
        End Sub

        Private Sub SetQuantity(value As Integer)
            _quantity = Math.Max(1, value)
            QtyBox.Text = _quantity.ToString()
            QtyBox.CaretIndex = QtyBox.Text.Length
            UpdatePreview()
        End Sub

        Private Sub UpdatePreview()
            Dim nextQty = If(_isStockIn, _currentQty + _quantity, _currentQty - _quantity)
            PreviewText.Text = $"New qty: {_currentQty} → {Math.Max(0, nextQty)}"
            HideError()
        End Sub

        Private Sub ConfirmSelection()
            Dim parsed As Integer
            If Not Integer.TryParse(QtyBox.Text, parsed) OrElse parsed < 1 Then
                ShowError("Enter a quantity of 1 or more.")
                QtyBox.Focus()
                QtyBox.SelectAll()
                Return
            End If

            If Not _isStockIn AndAlso parsed > _currentQty Then
                ShowError($"Cannot stock out more than {_currentQty} on hand.")
                QtyBox.Focus()
                QtyBox.SelectAll()
                Return
            End If

            ResultQuantity = parsed
            ResultReason = If(TryCast(ReasonBox.SelectedItem, String), String.Empty)
            ResultNotes = If(NotesBox.Text, String.Empty).Trim()
            Confirmed = True
            DialogResult = True
            Close()
        End Sub

        Private Sub CancelSelection()
            Confirmed = False
            DialogResult = False
            Close()
        End Sub

        Private Sub ShowError(message As String)
            ErrorText.Text = message
            ErrorText.Visibility = Visibility.Visible
        End Sub

        Private Sub HideError()
            ErrorText.Visibility = Visibility.Collapsed
        End Sub
    End Class
End Namespace
