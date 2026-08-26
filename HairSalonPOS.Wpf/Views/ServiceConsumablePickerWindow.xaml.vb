Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class ServiceConsumablePickerWindow
        Inherits Window

        Private ReadOnly _service As ServiceItem
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _slotControls As New List(Of PickOneSlotControl)

        Public Property Confirmed As Boolean
        Public Property Selections As New List(Of ServiceConsumableLine)

        Private Class PickOneSlotControl
            Public Property Recipe As ServiceConsumableLine
            Public Property ProductBox As ComboBox
            Public Property QtyBox As TextBox
        End Class

        Public Sub New(service As ServiceItem)
            InitializeComponent()
            _service = service
            TitleText.Text = $"Select products used — {If(service?.Name, "Service")}"
            BuildSlots()
        End Sub

        Private Sub BuildSlots()
            SlotsPanel.Items.Clear()
            _slotControls.Clear()

            If _service?.Consumables Is Nothing Then Return

            For Each recipe In _service.Consumables.Where(Function(c) c.Kind = ServiceConsumableKind.PickOne)
                Dim options = ResolveProducts(recipe.OptionProductSkus)
                If options.Count = 0 Then Continue For

                Dim panel As New StackPanel With {.Margin = New Thickness(0, 0, 0, 12)}

                Dim label As New TextBlock With {
                    .Text = "Product",
                    .Style = TryCast(FindResource("SectionHeader"), Style),
                    .Margin = New Thickness(0, 0, 0, 4)
                }
                panel.Children.Add(label)

                Dim productBox As New ComboBox With {
                    .Style = TryCast(FindResource("ModernComboBox"), Style),
                    .ItemsSource = options,
                    .DisplayMemberPath = NameOf(ProductItem.Name),
                    .SelectedValuePath = NameOf(ProductItem.Sku),
                    .Margin = New Thickness(0, 0, 0, 8)
                }
                productBox.SelectedIndex = 0
                panel.Children.Add(productBox)

                Dim qtyLabel As New TextBlock With {
                    .Text = "Quantity",
                    .Style = TryCast(FindResource("SectionHeader"), Style),
                    .Margin = New Thickness(0, 0, 0, 4)
                }
                panel.Children.Add(qtyLabel)

                Dim qtyBox As New TextBox With {
                    .Style = TryCast(FindResource("ModernTextBox"), Style),
                    .Text = If(recipe.Quantity > 0D, recipe.Quantity.ToString("0.##", CultureInfo.InvariantCulture), "1")
                }
                panel.Children.Add(qtyBox)

                SlotsPanel.Items.Add(panel)
                _slotControls.Add(New PickOneSlotControl With {
                    .Recipe = recipe,
                    .ProductBox = productBox,
                    .QtyBox = qtyBox
                })
            Next
        End Sub

        Private Function ResolveProducts(skus As IEnumerable(Of String)) As List(Of ProductItem)
            Dim allowed = If(skus, Enumerable.Empty(Of String)()).ToList()
            Return _store.Products.
                Where(Function(p) p.IsActive AndAlso allowed.Any(Function(s) s.Equals(p.Sku, StringComparison.OrdinalIgnoreCase))).
                OrderBy(Function(p) p.Name).
                ToList()
        End Function

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
            If _slotControls.Count = 0 Then
                ShowError("This service has no products to choose from. Set up a pick-at-POS recipe in Master Files.")
            End If
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

        Private Sub Confirm_Click(sender As Object, e As RoutedEventArgs)
            ConfirmSelection()
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            CancelSelection()
        End Sub

        Private Sub ConfirmSelection()
            HideError()
            Selections.Clear()

            For Each slot In _slotControls
                Dim sku = TryCast(slot.ProductBox.SelectedValue, String)
                If String.IsNullOrWhiteSpace(sku) Then
                    ShowError("Select a product for each choice.")
                    Return
                End If

                Dim qty As Decimal
                If Not Decimal.TryParse(slot.QtyBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, qty) OrElse qty <= 0D Then
                    ShowError("Enter a quantity greater than zero.")
                    slot.QtyBox.Focus()
                    Return
                End If

                Selections.Add(New ServiceConsumableLine With {
                    .Kind = ServiceConsumableKind.PickOne,
                    .ProductSku = sku,
                    .Quantity = qty
                })
            Next

            If Selections.Count = 0 Then
                ShowError("No product choices are available for this service.")
                Return
            End If

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
