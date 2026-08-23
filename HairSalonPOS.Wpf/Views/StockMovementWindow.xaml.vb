Imports System.Text.RegularExpressions
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class StockMovementWindow
        Inherits Window

        Private Shared ReadOnly DigitsOnly As New Regex("^\d+$")
        Private Shared ReadOnly StockInReasons As String() = {"Purchase", "Customer return", "Transfer in", "Other"}
        Private Shared ReadOnly StockOutReasons As String() = {"Damaged", "Expired", "Used in service", "Missing", "Return to supplier", "Other"}

        Private ReadOnly _isStockIn As Boolean
        Private _currentQty As Integer
        Private _quantity As Integer = 1
        Private _loadFailed As Boolean

        Public Property Confirmed As Boolean
        Public Property ResultQuantity As Integer
        Public Property ResultReason As String = String.Empty
        Public Property ResultNotes As String = String.Empty
        Public ReadOnly Property LoadSucceeded As Boolean
            Get
                Return Not _loadFailed
            End Get
        End Property

        Public Sub New(product As ProductItem, isStockIn As Boolean, Optional initialQty As Integer = 1)
            Try
                InitializeComponent()
            Catch ex As Exception
                ErrorLogService.LogException("StockMovementWindow/InitializeComponent", ex)
                Throw
            End Try

            _isStockIn = isStockIn

            If Not TryLoadProduct(product, isStockIn, initialQty) Then
                DisableFormControls()
            End If
        End Sub

        Private Function TryLoadProduct(product As ProductItem, isStockIn As Boolean, initialQty As Integer) As Boolean
            Try
                If product Is Nothing Then
                    ShowLoadError("Product details are missing.")
                    Return False
                End If

                product.EnsureDefaults()

                If String.IsNullOrWhiteSpace(product.Sku) Then
                    ShowLoadError("This product has no SKU. Edit it in Master Files or Inventory first.")
                    Return False
                End If

                If String.IsNullOrWhiteSpace(product.Name) Then
                    ShowLoadError("This product has no name. Edit it in Master Files or Inventory first.")
                    Return False
                End If

                _currentQty = product.StockOnHand

                TitleText.Text = If(isStockIn, "Stock in", "Stock out")
                ConfirmButton.Content = If(isStockIn, "Stock in", "Stock out")
                ProductNameText.Text = product.Name
                ProductMetaText.Text = $"SKU {product.Sku}  •  On hand {_currentQty}"

                Dim accentKey = If(isStockIn, "LinkStockInBrush", "LinkDeleteBrush")
                Dim accent = TryCast(TryFindResource(accentKey), Brush)
                If accent IsNot Nothing Then AccentBar.Background = accent

                ReasonBox.ItemsSource = If(isStockIn, StockInReasons, StockOutReasons)
                If ReasonBox.Items.Count > 0 Then ReasonBox.SelectedIndex = 0

                ApplyProductPhoto(product)

                SetQuantity(Math.Max(1, initialQty))
                Return True
            Catch ex As Exception
                ErrorLogService.LogException($"StockMovementWindow/TryLoadProduct — {If(product?.Sku, "(null)")}", ex)
                ShowLoadError($"Could not load product details.{Environment.NewLine}{Environment.NewLine}{ErrorLogService.Describe(ex)}")
                Return False
            End Try
        End Function

        Private Sub ApplyProductPhoto(product As ProductItem)
            Dim photo As ImageSource = Nothing
            Try
                photo = CatalogImageService.Instance.CreateImageSource(product.ImagePath)
            Catch ex As Exception
                ErrorLogService.LogException($"StockMovementWindow/LoadPhoto — {product.Sku}", ex)
            End Try

            If photo IsNot Nothing Then
                ProductPhoto.Source = photo
                ProductPhoto.Visibility = Visibility.Visible
                PhotoPlaceholder.Visibility = Visibility.Collapsed
                Return
            End If

            ProductPhoto.Visibility = Visibility.Collapsed
            PhotoPlaceholder.Visibility = Visibility.Visible
            PhotoPlaceholder.Text = ProductPlaceholderIcons.Resolve(product)
            PhotoPlaceholder.FontSize = 28
        End Sub

        Private Sub ShowLoadError(message As String)
            _loadFailed = True
            If ErrorText Is Nothing Then Return
            ErrorText.Text = message
            ErrorText.Visibility = Visibility.Visible
        End Sub

        Private Sub DisableFormControls()
            ConfirmButton.IsEnabled = False
            QtyBox.IsEnabled = False
            ReasonBox.IsEnabled = False
            NotesBox.IsEnabled = False
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            Try
                AppDialogService.ApplyOwnerOverlaySizing(Me)
                If _loadFailed Then Return
                Dispatcher.BeginInvoke(Sub()
                                           Try
                                               QtyBox.Focus()
                                               QtyBox.SelectAll()
                                           Catch ex As Exception
                                               ErrorLogService.LogException("StockMovementWindow/FocusQty", ex)
                                           End Try
                                       End Sub)
            Catch ex As Exception
                ErrorLogService.LogException("StockMovementWindow/Window_Loaded", ex)
            End Try
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
            Dim box = TryCast(sender, TextBox)
            If box Is Nothing Then Return

            Dim parsed As Integer
            If Integer.TryParse(box.Text, parsed) Then
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

        ''' <summary>
        ''' QtyBox raises TextChanged while BAML is still building the tree, so PreviewText and
        ''' ErrorText can still be Nothing here. TryLoadProduct calls SetQuantity once the tree is
        ''' complete, which recomputes the preview.
        ''' </summary>
        Private Sub UpdatePreview()
            If _loadFailed OrElse PreviewText Is Nothing Then Return
            Dim nextQty = If(_isStockIn, _currentQty + _quantity, _currentQty - _quantity)
            PreviewText.Text = $"New qty: {_currentQty} → {Math.Max(0, nextQty)}"
            HideError()
        End Sub

        Private Sub ConfirmSelection()
            If _loadFailed Then
                CancelSelection()
                Return
            End If

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
            If ErrorText Is Nothing Then Return
            ErrorText.Text = message
            ErrorText.Visibility = Visibility.Visible
        End Sub

        Private Sub HideError()
            If _loadFailed OrElse ErrorText Is Nothing Then Return
            ErrorText.Visibility = Visibility.Collapsed
        End Sub
    End Class
End Namespace
