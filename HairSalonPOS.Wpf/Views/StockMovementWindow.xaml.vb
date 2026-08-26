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
        Private Shared ReadOnly AddReserveStockReasons As String() = {"Building backup stock", "Seasonal buffer", "Safety stock", "Other"}
        Private Shared ReadOnly UseReserveStockReasons As String() = {"Typhoon / disaster", "Late delivery", "Supplier delay", "Cannot reorder", "Other"}

        Private ReadOnly _kind As StockMovementKind
        Private _currentQty As Integer
        Private _reservedQty As Integer
        Private _quantity As Integer = 1
        Private _loadFailed As Boolean
        Private _isUseReserveStock As Boolean

        Public Property Confirmed As Boolean
        Public Property ResultQuantity As Integer
        Public Property ResultReason As String = String.Empty
        Public Property ResultNotes As String = String.Empty
        Public Property ResultIsReleaseReserve As Boolean
        Public ReadOnly Property LoadSucceeded As Boolean
            Get
                Return Not _loadFailed
            End Get
        End Property

        Public Sub New(product As ProductItem, isStockIn As Boolean, Optional initialQty As Integer = 1)
            Me.New(product, If(isStockIn, StockMovementKind.StockIn, StockMovementKind.StockOut), initialQty)
        End Sub

        Public Sub New(product As ProductItem, kind As StockMovementKind, Optional initialQty As Integer = 1)
            Try
                InitializeComponent()
            Catch ex As Exception
                ErrorLogService.LogException("StockMovementWindow/InitializeComponent", ex)
                Throw
            End Try

            _kind = kind

            If Not TryLoadProduct(product, initialQty) Then
                DisableFormControls()
            End If
        End Sub

        Private Function TryLoadProduct(product As ProductItem, initialQty As Integer) As Boolean
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
                _reservedQty = product.ReservedQty

                ConfigureForKind()
                ProductNameText.Text = product.Name
                RefreshProductMetaText(product.Sku)
                ApplyProductPhoto(product)
                SetQuantity(Math.Max(1, initialQty))
                UpdateReserveModeAvailability()
                Return True
            Catch ex As Exception
                ErrorLogService.LogException($"StockMovementWindow/TryLoadProduct — {If(product?.Sku, "(null)")}", ex)
                ShowLoadError($"Could not load product details.{Environment.NewLine}{Environment.NewLine}{ErrorLogService.Describe(ex)}")
                Return False
            End Try
        End Function

        Private Sub ConfigureForKind()
            Select Case _kind
                Case StockMovementKind.StockIn
                    TitleText.Text = "Stock in"
                    ConfirmButton.Content = "Stock in"
                    ReserveModePanel.Visibility = Visibility.Collapsed
                    ReasonBox.ItemsSource = StockInReasons
                    Dim accent = TryCast(TryFindResource("LinkStockInBrush"), Brush)
                    If accent IsNot Nothing Then AccentBar.Background = accent
                Case StockMovementKind.StockOut
                    TitleText.Text = "Stock out"
                    ConfirmButton.Content = "Stock out"
                    ReserveModePanel.Visibility = Visibility.Collapsed
                    ReasonBox.ItemsSource = StockOutReasons
                    Dim accent = TryCast(TryFindResource("LinkDeleteBrush"), Brush)
                    If accent IsNot Nothing Then AccentBar.Background = accent
                Case StockMovementKind.Reserve
                    TitleText.Text = "Reserve Stock"
                    ReserveModePanel.Visibility = Visibility.Visible
                    If _currentQty <= 0 AndAlso _reservedQty > 0 Then
                        ReleaseRadio.IsChecked = True
                        _isUseReserveStock = True
                    Else
                        ReserveRadio.IsChecked = True
                        _isUseReserveStock = False
                    End If
                    ApplyReserveStockMode()
                    Dim accent = TryCast(TryFindResource("AccentBrush"), Brush)
                    If accent IsNot Nothing Then AccentBar.Background = accent
            End Select

            If ReasonBox.Items.Count > 0 Then ReasonBox.SelectedIndex = 0
        End Sub

        Private Sub ApplyReserveStockMode()
            If _isUseReserveStock Then
                ConfirmButton.Content = "Use reserve stock"
                ReasonBox.ItemsSource = UseReserveStockReasons
            Else
                ConfirmButton.Content = "Add to reserve stock"
                ReasonBox.ItemsSource = AddReserveStockReasons
            End If
            If ReasonBox.Items.Count > 0 Then ReasonBox.SelectedIndex = 0
        End Sub

        Private Sub UpdateReserveModeAvailability()
            If _kind <> StockMovementKind.Reserve Then Return
            ReserveRadio.IsEnabled = True
            ReleaseRadio.IsEnabled = _reservedQty > 0 AndAlso _currentQty <= 0
            If Not ReleaseRadio.IsEnabled AndAlso ReserveRadio.IsEnabled Then
                ReserveRadio.IsChecked = True
                _isUseReserveStock = False
                ApplyReserveStockMode()
            ElseIf Not ReserveRadio.IsEnabled AndAlso ReleaseRadio.IsEnabled Then
                ReleaseRadio.IsChecked = True
                _isUseReserveStock = True
                ApplyReserveStockMode()
            End If
        End Sub

        Private Sub RefreshProductMetaText(productSku As String)
            If _kind = StockMovementKind.Reserve Then
                ProductMetaText.Text = $"On hand {_currentQty}  •  {_reservedQty} reserve stock"
            Else
                ProductMetaText.Text = $"SKU {productSku}  •  On hand {_currentQty}"
            End If
        End Sub

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

        Private Sub ReserveMode_Changed(sender As Object, e As RoutedEventArgs)
            If _kind <> StockMovementKind.Reserve OrElse _loadFailed Then Return
            _isUseReserveStock = ReleaseRadio.IsChecked = True
            If _isUseReserveStock AndAlso _currentQty > 0 Then
                ShowError("Use reserve stock only when on-hand is depleted.")
                ReserveRadio.IsChecked = True
                _isUseReserveStock = False
            End If
            ApplyReserveStockMode()
            UpdatePreview()
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

        Private Sub UpdatePreview()
            If _loadFailed OrElse PreviewText Is Nothing Then Return

            Select Case _kind
                Case StockMovementKind.StockIn
                    Dim nextQty = _currentQty + _quantity
                    PreviewText.Text = $"On hand: {_currentQty} → {nextQty}"
                Case StockMovementKind.StockOut
                    Dim nextQty = _currentQty - _quantity
                    PreviewText.Text = $"On hand: {_currentQty} → {Math.Max(0, nextQty)}"
                Case StockMovementKind.Reserve
                    If _isUseReserveStock Then
                        Dim nextOnHand = _currentQty + _quantity
                        Dim nextReserve = Math.Max(0, _reservedQty - _quantity)
                        PreviewText.Text = $"On hand: {_currentQty} → {nextOnHand}  •  Reserve stock: {_reservedQty} → {nextReserve}"
                    Else
                        Dim nextReserve = _reservedQty + _quantity
                        PreviewText.Text = $"On hand: {_currentQty} (unchanged)  •  Reserve stock: {_reservedQty} → {nextReserve}"
                    End If
            End Select

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

            Select Case _kind
                Case StockMovementKind.StockOut
                    If parsed > _currentQty Then
                        ShowError($"Cannot stock out more than {_currentQty} on hand.")
                        QtyBox.Focus()
                        QtyBox.SelectAll()
                        Return
                    End If
                Case StockMovementKind.Reserve
                    If _isUseReserveStock Then
                        If _currentQty > 0 Then
                            ShowError("Reserve stock can only be used when on-hand is depleted.")
                            Return
                        End If
                        If parsed > _reservedQty Then
                            ShowError($"Cannot use more than {_reservedQty} from reserve stock.")
                            QtyBox.Focus()
                            QtyBox.SelectAll()
                            Return
                        End If
                    End If
            End Select

            ResultQuantity = parsed
            ResultReason = If(TryCast(ReasonBox.SelectedItem, String), String.Empty)
            ResultNotes = If(NotesBox.Text, String.Empty).Trim()
            ResultIsReleaseReserve = _isUseReserveStock
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
