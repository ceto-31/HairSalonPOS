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
        Private Shared ReadOnly StockOutReasons As String() = {"Damaged", "Expired", "Used in service", "Missing", "Return to supplier", "Other"}

        Private ReadOnly _kind As StockMovementKind
        Private _product As ProductItem
        Private _currentQty As Integer
        Private _reservedQty As Integer
        Private _quantity As Integer = 1
        Private _unitsPerBox As Integer = 1
        Private _unitLabel As String = "pc"
        Private _boxLabel As String = "box"
        Private _loadFailed As Boolean
        Private _isUseReserveStock As Boolean
        Private _enterAsBoxes As Boolean
        Private _stockOutBatches As List(Of StockOutBatchOption) = New List(Of StockOutBatchOption)()
        Private _selectedStockOutBatch As StockOutBatchOption
        Private ReadOnly _stockOutOptions As StockMovementStockOutOptions
        Private ReadOnly _stockOutFromReserve As Boolean
        Private ReadOnly _preselectedBatchId As Integer?
        Private ReadOnly _lockBatchSelection As Boolean
        Private ReadOnly _defaultReason As String

        Public Property Confirmed As Boolean
        Public Property ResultQuantity As Integer
        Public Property ResultQuantityPieces As Integer
        Public Property ResultBoxesReceived As Integer
        Public Property ResultReason As String = String.Empty
        Public Property ResultNotes As String = String.Empty
        Public Property ResultIsReleaseReserve As Boolean
        Public Property ResultExpirationDate As Date?
        Public Property ResultBoxCode As String = String.Empty
        Public Property ResultBatchId As Integer?
        Public ReadOnly Property LoadSucceeded As Boolean
            Get
                Return Not _loadFailed
            End Get
        End Property

        Public Sub New(product As ProductItem, isStockIn As Boolean, Optional initialQty As Integer = 1)
            Me.New(product, If(isStockIn, StockMovementKind.StockIn, StockMovementKind.StockOut), initialQty, Nothing)
        End Sub

        Public Sub New(product As ProductItem, kind As StockMovementKind, Optional initialQty As Integer = 1,
                       Optional stockOutOptions As StockMovementStockOutOptions = Nothing)
            Try
                InitializeComponent()
            Catch ex As Exception
                ErrorLogService.LogException("StockMovementWindow/InitializeComponent", ex)
                Throw
            End Try

            _kind = kind
            _stockOutOptions = stockOutOptions
            _stockOutFromReserve = stockOutOptions IsNot Nothing AndAlso stockOutOptions.FromReserve
            _preselectedBatchId = stockOutOptions?.PreselectedBatchId
            _lockBatchSelection = stockOutOptions IsNot Nothing AndAlso stockOutOptions.LockBatchSelection
            _defaultReason = If(stockOutOptions?.DefaultReason, String.Empty)

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

                _product = product
                _currentQty = product.StockOnHand
                _reservedQty = product.ReservedQty
                _unitsPerBox = Math.Max(1, product.UnitsPerBox)
                _unitLabel = If(String.IsNullOrWhiteSpace(product.UnitLabel), "pc", product.UnitLabel.Trim())
                _boxLabel = If(String.IsNullOrWhiteSpace(product.BoxLabel), "box", product.BoxLabel.Trim())

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
                    ShowStockInBatchPanel(True, showBoxCode:=True)
                    ShowStockOutBatchPanel(False)
                    ShowReasonPanel(False)
                    Dim accent = TryCast(TryFindResource("LinkStockInBrush"), Brush)
                    If accent IsNot Nothing Then AccentBar.Background = accent
                Case StockMovementKind.StockOut
                    TitleText.Text = "Stock out"
                    ConfirmButton.Content = "Stock out"
                    ReserveModePanel.Visibility = Visibility.Collapsed
                    ShowStockInBatchPanel(False)
                    LoadStockOutBatches()
                    ShowStockOutBatchPanel(True)
                    ShowReasonPanel(True)
                    ReasonBox.ItemsSource = StockOutReasons
                    ApplyDefaultStockOutReason()
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

            If ReasonBox.Items.Count > 0 AndAlso String.IsNullOrWhiteSpace(_defaultReason) Then
                ReasonBox.SelectedIndex = 0
            End If
            UpdateEntryUnitPanel()
        End Sub

        Private Sub ShowStockInBatchPanel(show As Boolean, Optional showBoxCode As Boolean = True)
            If StockInBatchPanel Is Nothing Then Return
            StockInBatchPanel.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
            If BoxCodePanel IsNot Nothing Then
                BoxCodePanel.Visibility = If(show AndAlso showBoxCode, Visibility.Visible, Visibility.Collapsed)
            End If
            If show AndAlso StockInExpirationDatePicker IsNot Nothing AndAlso Not StockInExpirationDatePicker.SelectedDate.HasValue Then
                StockInExpirationDatePicker.SelectedDate = Date.Today.AddYears(1)
            End If
            If show AndAlso showBoxCode AndAlso BoxCodeBox IsNot Nothing Then
                BoxCodeBox.Text = String.Empty
            End If
        End Sub

        Private Sub ShowStockOutBatchPanel(show As Boolean)
            If StockOutBatchPanel Is Nothing Then Return
            StockOutBatchPanel.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
            If Not show Then
                _selectedStockOutBatch = Nothing
                If StockOutBatchDetailText IsNot Nothing Then StockOutBatchDetailText.Text = String.Empty
                If StockOutExpirationDatePicker IsNot Nothing Then StockOutExpirationDatePicker.SelectedDate = Nothing
            End If
        End Sub

        Private Sub LoadStockOutBatches()
            Dim store = InMemoryDataStore.Instance
            Dim sourceBatches = If(_stockOutFromReserve,
                                   store.GetAvailableReserveBatches(_product.Sku),
                                   store.GetAvailableOnHandBatches(_product.Sku))

            _stockOutBatches = sourceBatches.
                Select(Function(b) New StockOutBatchOption With {
                    .BatchId = b.BatchId,
                    .BoxCode = b.BoxCode,
                    .QuantityRemaining = b.QuantityRemaining,
                    .ExpirationDate = b.ExpirationDate
                }).ToList()

            If StockOutBatchBox Is Nothing Then Return
            StockOutBatchBox.ItemsSource = _stockOutBatches
            StockOutBatchBox.IsEnabled = _stockOutBatches.Count > 0

            If _stockOutBatches.Count = 0 Then
                StockOutBatchBox.SelectedIndex = -1
                _selectedStockOutBatch = Nothing
                If StockOutBatchDetailText IsNot Nothing Then
                    StockOutBatchDetailText.Text = If(_stockOutFromReserve,
                        "No available reserve batches with stock for this product.",
                        "No available batches with stock for this product.")
                End If
                Return
            End If

            Dim selectedIndex = 0
            If _preselectedBatchId.HasValue Then
                Dim matchIndex = _stockOutBatches.FindIndex(Function(b) b.BatchId = _preselectedBatchId.Value)
                If matchIndex >= 0 Then selectedIndex = matchIndex
            End If

            StockOutBatchBox.SelectedIndex = selectedIndex
            If _lockBatchSelection Then StockOutBatchBox.IsEnabled = False
        End Sub

        Private Sub ApplyDefaultStockOutReason()
            If ReasonBox Is Nothing OrElse String.IsNullOrWhiteSpace(_defaultReason) Then Return
            Dim reasonIndex = Array.IndexOf(StockOutReasons, _defaultReason)
            If reasonIndex >= 0 Then ReasonBox.SelectedIndex = reasonIndex
        End Sub

        Private Sub StockOutBatchBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            _selectedStockOutBatch = TryCast(StockOutBatchBox?.SelectedItem, StockOutBatchOption)
            If StockOutBatchDetailText IsNot Nothing Then
                StockOutBatchDetailText.Text = If(_selectedStockOutBatch?.DetailText, String.Empty)
            End If
            If StockOutExpirationDatePicker IsNot Nothing Then
                StockOutExpirationDatePicker.SelectedDate = _selectedStockOutBatch?.ExpirationDate
            End If
            UpdatePreview()
        End Sub

        Private Sub ShowReasonPanel(show As Boolean)
            If ReasonPanel Is Nothing Then Return
            ReasonPanel.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub ApplyReserveStockMode()
            If _isUseReserveStock Then
                ConfirmButton.Content = "Use reserve stock"
                ShowStockInBatchPanel(False)
                ShowStockOutBatchPanel(False)
                ShowReasonPanel(False)
            Else
                ConfirmButton.Content = "Add to reserve stock"
                ShowStockInBatchPanel(True, showBoxCode:=True)
                ShowStockOutBatchPanel(False)
                ShowReasonPanel(False)
            End If
            UpdateEntryUnitPanel()
        End Sub

        Private Function SupportsBoxEntry() As Boolean
            Return _unitsPerBox > 1 AndAlso ShowsEntryUnitToggle()
        End Function

        Private Function ShowsEntryUnitToggle() As Boolean
            If _kind = StockMovementKind.StockIn Then Return True
            Return _kind = StockMovementKind.Reserve AndAlso Not _isUseReserveStock
        End Function

        Private Sub UpdateEntryUnitPanel()
            If EntryUnitPanel Is Nothing Then Return
            Dim show = SupportsBoxEntry()
            EntryUnitPanel.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
            If Not show Then
                _enterAsBoxes = False
                If PiecesRadio IsNot Nothing Then PiecesRadio.IsChecked = True
            Else
                _enterAsBoxes = BoxesRadio IsNot Nothing AndAlso BoxesRadio.IsChecked = True
            End If
            RefreshQuantityHeader()
            UpdatePreview()
        End Sub

        Private Sub RefreshQuantityHeader()
            If QuantityHeaderText Is Nothing Then Return
            If SupportsBoxEntry() AndAlso _enterAsBoxes Then
                QuantityHeaderText.Text = $"Quantity ({_boxLabel})"
            ElseIf SupportsBoxEntry() Then
                QuantityHeaderText.Text = $"Quantity ({_unitLabel})"
            Else
                QuantityHeaderText.Text = "Quantity"
            End If
        End Sub

        Private Sub EntryUnit_Changed(sender As Object, e As RoutedEventArgs)
            If _loadFailed Then Return
            _enterAsBoxes = BoxesRadio IsNot Nothing AndAlso BoxesRadio.IsChecked = True
            RefreshQuantityHeader()
            UpdatePreview()
        End Sub

        Private Function EffectivePieces(enteredQty As Integer) As Integer
            If SupportsBoxEntry() AndAlso _enterAsBoxes Then Return enteredQty * _unitsPerBox
            Return enteredQty
        End Function

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
            ElseIf _kind = StockMovementKind.StockOut AndAlso _stockOutFromReserve Then
                ProductMetaText.Text = $"SKU {productSku}  •  {_reservedQty} reserve stock"
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
            If ReasonPanel IsNot Nothing Then ReasonBox.IsEnabled = False
            If StockInBatchPanel IsNot Nothing Then
                BoxCodeBox.IsEnabled = False
                StockInExpirationDatePicker.IsEnabled = False
            End If
            If StockOutBatchPanel IsNot Nothing Then
                StockOutBatchBox.IsEnabled = False
                If StockOutExpirationDatePicker IsNot Nothing Then StockOutExpirationDatePicker.IsEnabled = False
            End If
            If EntryUnitPanel IsNot Nothing Then
                PiecesRadio.IsEnabled = False
                BoxesRadio.IsEnabled = False
            End If
            NotesBox.IsEnabled = False
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            Try
                ApplyOverlaySizing()
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

        Private Sub ApplyOverlaySizing()
            AppDialogService.ApplyOwnerOverlaySizing(Me)
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

            Dim pieces = EffectivePieces(_quantity)
            UpdateConversionPreview(pieces)

            Select Case _kind
                Case StockMovementKind.StockIn
                    Dim nextQty = _currentQty + pieces
                    PreviewText.Text = $"On hand: {_currentQty} → {nextQty}"
                Case StockMovementKind.StockOut
                    If _selectedStockOutBatch IsNot Nothing Then
                        Dim batchNext = Math.Max(0, _selectedStockOutBatch.QuantityRemaining - pieces)
                        If _stockOutFromReserve Then
                            Dim nextReserve = Math.Max(0, _reservedQty - pieces)
                            PreviewText.Text =
                                $"Batch available: {_selectedStockOutBatch.QuantityRemaining} → {batchNext}  •  Reserve stock: {_reservedQty} → {nextReserve}"
                        Else
                            Dim nextQty = _currentQty - pieces
                            PreviewText.Text =
                                $"Batch available: {_selectedStockOutBatch.QuantityRemaining} → {batchNext}  •  On hand: {_currentQty} → {Math.Max(0, nextQty)}"
                        End If
                    ElseIf _stockOutFromReserve Then
                        PreviewText.Text = $"Reserve stock: {_reservedQty} → {Math.Max(0, _reservedQty - pieces)}"
                    Else
                        Dim nextQty = _currentQty - pieces
                        PreviewText.Text = $"On hand: {_currentQty} → {Math.Max(0, nextQty)}"
                    End If
                Case StockMovementKind.Reserve
                    If _isUseReserveStock Then
                        Dim nextOnHand = _currentQty + pieces
                        Dim nextReserve = Math.Max(0, _reservedQty - pieces)
                        PreviewText.Text = $"On hand: {_currentQty} → {nextOnHand}  •  Reserve stock: {_reservedQty} → {nextReserve}"
                    Else
                        Dim nextReserve = _reservedQty + pieces
                        PreviewText.Text = $"On hand: {_currentQty} (unchanged)  •  Reserve stock: {_reservedQty} → {nextReserve}"
                    End If
            End Select

            HideError()
        End Sub

        Private Sub UpdateConversionPreview(pieces As Integer)
            If ConversionPreviewText Is Nothing Then Return

            If SupportsBoxEntry() AndAlso _enterAsBoxes Then
                ConversionPreviewText.Visibility = Visibility.Visible
                ConversionPreviewText.Text =
                    $"= {pieces} {_unitLabel} ({_quantity} {_boxLabel} × {_unitsPerBox}/{_unitLabel})"
            ElseIf SupportsBoxEntry() AndAlso _unitsPerBox > 1 AndAlso pieces >= _unitsPerBox Then
                Dim wholeBoxes = pieces \ _unitsPerBox
                Dim remainder = pieces Mod _unitsPerBox
                ConversionPreviewText.Visibility = Visibility.Visible
                If remainder = 0 Then
                    ConversionPreviewText.Text = $"= {pieces} {_unitLabel} ({wholeBoxes} {_boxLabel} × {_unitsPerBox}/{_unitLabel})"
                Else
                    ConversionPreviewText.Text = $"= {pieces} {_unitLabel} ({wholeBoxes} {_boxLabel} + {remainder} {_unitLabel})"
                End If
            Else
                ConversionPreviewText.Visibility = Visibility.Collapsed
                ConversionPreviewText.Text = String.Empty
            End If
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

            Dim pieces = EffectivePieces(parsed)

            Select Case _kind
                Case StockMovementKind.StockOut
                    If _selectedStockOutBatch Is Nothing Then
                        ShowError("Select a box code with available stock.")
                        StockOutBatchBox?.Focus()
                        Return
                    End If
                    If pieces > _selectedStockOutBatch.QuantityRemaining Then
                        ShowError($"Cannot stock out more than {_selectedStockOutBatch.QuantityRemaining} from the selected batch.")
                        QtyBox.Focus()
                        QtyBox.SelectAll()
                        Return
                    End If
                    If _stockOutFromReserve Then
                        If pieces > _reservedQty Then
                            ShowError($"Cannot stock out more than {_reservedQty} from reserve stock.")
                            QtyBox.Focus()
                            QtyBox.SelectAll()
                            Return
                        End If
                    ElseIf pieces > _currentQty Then
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
                        If pieces > _reservedQty Then
                            ShowError($"Cannot use more than {_reservedQty} from reserve stock.")
                            QtyBox.Focus()
                            QtyBox.SelectAll()
                            Return
                        End If
                    End If
            End Select

            If RequiresExpirationDate() Then
                Dim datePicker = ActiveExpirationDatePicker()
                If datePicker Is Nothing OrElse Not datePicker.SelectedDate.HasValue Then
                    ShowError("Select an expiration date.")
                    datePicker?.Focus()
                    Return
                End If
                ResultExpirationDate = datePicker.SelectedDate.Value.Date
            Else
                ResultExpirationDate = Nothing
            End If

            If _kind = StockMovementKind.StockIn OrElse
               (_kind = StockMovementKind.Reserve AndAlso Not _isUseReserveStock) Then
                ResultBoxCode = If(BoxCodeBox?.Text, String.Empty).Trim()
                ResultBatchId = Nothing
            ElseIf _kind = StockMovementKind.StockOut Then
                ResultBoxCode = If(_selectedStockOutBatch?.BoxCode, String.Empty).Trim()
                ResultBatchId = _selectedStockOutBatch?.BatchId
                ResultExpirationDate = _selectedStockOutBatch?.ExpirationDate
            Else
                ResultBoxCode = String.Empty
                ResultBatchId = Nothing
            End If

            ResultQuantity = parsed
            ResultQuantityPieces = pieces
            ResultBoxesReceived = If(SupportsBoxEntry() AndAlso _enterAsBoxes, parsed, 0)
            ResultReason = If(UsesReasonField(), If(TryCast(ReasonBox.SelectedItem, String), String.Empty), String.Empty)
            ResultNotes = If(NotesBox.Text, String.Empty).Trim()
            ResultIsReleaseReserve = _isUseReserveStock
            Confirmed = True
            DialogResult = True
            Close()
        End Sub

        Private Function ActiveExpirationDatePicker() As DatePicker
            If _kind = StockMovementKind.StockIn Then Return StockInExpirationDatePicker
            If _kind = StockMovementKind.Reserve AndAlso Not _isUseReserveStock Then Return StockInExpirationDatePicker
            Return Nothing
        End Function

        Private Function RequiresExpirationDate() As Boolean
            If _kind = StockMovementKind.StockIn Then Return True
            Return _kind = StockMovementKind.Reserve AndAlso Not _isUseReserveStock
        End Function

        Private Function UsesReasonField() As Boolean
            Return _kind = StockMovementKind.StockOut
        End Function

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
