Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Public Enum ServiceConsumablesSelectionMode
        Fixed = 0
        PickOne = 1
    End Enum

    Partial Public Class ServiceConsumablesSelectionWindow
        Inherits Window

        Private ReadOnly _mode As ServiceConsumablesSelectionMode
        Private ReadOnly _fixedOptions As ObservableCollection(Of FixedConsumableOption)
        Private ReadOnly _pickOneOptions As ObservableCollection(Of PickOneProductOption)
        Private ReadOnly _productsView As ListCollectionView

        Public Property Confirmed As Boolean
        Public Property ResultFixedOptions As List(Of FixedConsumableOption)
        Public Property ResultPickOneOptions As List(Of PickOneProductOption)
        Public Property ResultPickOneDefaultQty As Decimal = 1D

        Public Sub New(mode As ServiceConsumablesSelectionMode,
                       products As IEnumerable(Of ProductItem),
                       fixedOptions As IEnumerable(Of FixedConsumableOption),
                       pickOneOptions As IEnumerable(Of PickOneProductOption),
                       pickOneDefaultQty As Decimal)
            InitializeComponent()
            _mode = mode

            If mode = ServiceConsumablesSelectionMode.Fixed Then
                TitleText.Text = "Always deduct products"
                SubtitleText.Text = "Select products that deduct from inventory every time this service is sold."
                _fixedOptions = New ObservableCollection(Of FixedConsumableOption)(
                    BuildFixedOptions(products, fixedOptions))
                _productsView = New ListCollectionView(_fixedOptions)
                ProductsPanel.ItemsSource = _productsView
                BuildFixedRowTemplate()
            Else
                TitleText.Text = "Pick-at-POS products"
                SubtitleText.Text = "Select products the customer can choose from at Point of Sale. Inventory deducts after the cashier picks one."
                PickOneDefaultQtyPanel.Visibility = Visibility.Visible
                DefaultQtyBox.Text = If(pickOneDefaultQty > 0D, pickOneDefaultQty.ToString("0.##", CultureInfo.InvariantCulture), "1")
                _pickOneOptions = New ObservableCollection(Of PickOneProductOption)(
                    BuildPickOneOptions(products, pickOneOptions))
                _productsView = New ListCollectionView(_pickOneOptions)
                ProductsPanel.ItemsSource = _productsView
                BuildPickOneRowTemplate()
            End If
        End Sub

        Private Shared Function BuildFixedOptions(products As IEnumerable(Of ProductItem),
                                                  current As IEnumerable(Of FixedConsumableOption)) As IEnumerable(Of FixedConsumableOption)
            Dim bySku = If(current, Enumerable.Empty(Of FixedConsumableOption)()).
                GroupBy(Function(o) o.Sku, StringComparer.OrdinalIgnoreCase).
                ToDictionary(Function(g) g.Key, Function(g) g.First(), StringComparer.OrdinalIgnoreCase)

            Return If(products, Enumerable.Empty(Of ProductItem)()).
                OrderBy(Function(p) p.Name).
                Select(Function(p)
                           Dim existing As FixedConsumableOption = Nothing
                           bySku.TryGetValue(p.Sku, existing)
                           Return New FixedConsumableOption With {
                               .Sku = p.Sku,
                               .Name = p.Name,
                               .IsSelected = existing IsNot Nothing,
                               .Quantity = If(existing IsNot Nothing AndAlso existing.Quantity > 0D, existing.Quantity, 1D)
                           }
                       End Function)
        End Function

        Private Shared Function BuildPickOneOptions(products As IEnumerable(Of ProductItem),
                                                    current As IEnumerable(Of PickOneProductOption)) As IEnumerable(Of PickOneProductOption)
            Dim selected = New HashSet(Of String)(
                If(current, Enumerable.Empty(Of PickOneProductOption)()).
                    Where(Function(o) o.IsSelected).
                    Select(Function(o) o.Sku),
                StringComparer.OrdinalIgnoreCase)

            Return If(products, Enumerable.Empty(Of ProductItem)()).
                OrderBy(Function(p) p.Name).
                Select(Function(p) New PickOneProductOption With {
                    .Sku = p.Sku,
                    .Name = p.Name,
                    .IsSelected = selected.Contains(p.Sku)
                })
        End Function

        Private Sub BuildFixedRowTemplate()
            Dim template As New DataTemplate()
            Dim gridFactory As New FrameworkElementFactory(GetType(Grid))
            gridFactory.SetValue(Grid.MarginProperty, New Thickness(0, 0, 0, 6))

            Dim col0 As New FrameworkElementFactory(GetType(ColumnDefinition))
            col0.SetValue(ColumnDefinition.WidthProperty, New GridLength(1, GridUnitType.Star))
            Dim col1 As New FrameworkElementFactory(GetType(ColumnDefinition))
            col1.SetValue(ColumnDefinition.WidthProperty, New GridLength(72))
            gridFactory.AppendChild(col0)
            gridFactory.AppendChild(col1)

            Dim checkFactory As New FrameworkElementFactory(GetType(CheckBox))
            checkFactory.SetBinding(CheckBox.IsCheckedProperty, New Binding(NameOf(FixedConsumableOption.IsSelected)) With {.Mode = BindingMode.TwoWay})
            checkFactory.SetBinding(CheckBox.ContentProperty, New Binding(NameOf(FixedConsumableOption.Name)))
            checkFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center)
            checkFactory.SetValue(Grid.ColumnProperty, 0)
            gridFactory.AppendChild(checkFactory)

            Dim qtyFactory As New FrameworkElementFactory(GetType(TextBox))
            qtyFactory.SetBinding(TextBox.TextProperty, New Binding(NameOf(FixedConsumableOption.Quantity)) With {
                .Mode = BindingMode.TwoWay,
                .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                .StringFormat = "0.##"
            })
            qtyFactory.SetValue(TextBox.MarginProperty, New Thickness(8, 0, 0, 0))
            qtyFactory.SetValue(Grid.ColumnProperty, 1)
            qtyFactory.SetValue(FrameworkElement.StyleProperty, FindResource("ModernTextBox"))
            qtyFactory.SetValue(FrameworkElement.ToolTipProperty, "Qty per service")
            gridFactory.AppendChild(qtyFactory)

            template.VisualTree = gridFactory
            ProductsPanel.ItemTemplate = template
        End Sub

        Private Sub BuildPickOneRowTemplate()
            Dim template As New DataTemplate()
            Dim checkFactory As New FrameworkElementFactory(GetType(CheckBox))
            checkFactory.SetBinding(CheckBox.IsCheckedProperty, New Binding(NameOf(PickOneProductOption.IsSelected)) With {.Mode = BindingMode.TwoWay})
            checkFactory.SetBinding(CheckBox.ContentProperty, New Binding(NameOf(PickOneProductOption.Name)))
            checkFactory.SetValue(CheckBox.MarginProperty, New Thickness(0, 0, 0, 4))
            template.VisualTree = checkFactory
            ProductsPanel.ItemTemplate = template
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
            SearchBox.Focus()
        End Sub

        Private Sub SearchBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            ApplySearchFilter()
        End Sub

        Private Sub ApplySearchFilter()
            Dim term = If(SearchBox.Text, String.Empty).Trim()
            _productsView.Filter = Function(item)
                                       If String.IsNullOrEmpty(term) Then Return True
                                       Dim name = GetItemName(item)
                                       Return Not String.IsNullOrEmpty(name) AndAlso
                                              name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                                   End Function
        End Sub

        Private Shared Function GetItemName(item As Object) As String
            Dim fixed = TryCast(item, FixedConsumableOption)
            If fixed IsNot Nothing Then Return fixed.Name
            Dim pick = TryCast(item, PickOneProductOption)
            If pick IsNot Nothing Then Return pick.Name
            Return String.Empty
        End Function

        Private Sub SelectAll_Click(sender As Object, e As RoutedEventArgs)
            HideError()
            If _mode = ServiceConsumablesSelectionMode.Fixed Then
                For Each opt In _fixedOptions
                    opt.IsSelected = True
                Next
            Else
                For Each opt In _pickOneOptions
                    opt.IsSelected = True
                Next
            End If
        End Sub

        Private Sub Clear_Click(sender As Object, e As RoutedEventArgs)
            HideError()
            If _mode = ServiceConsumablesSelectionMode.Fixed Then
                For Each opt In _fixedOptions
                    opt.IsSelected = False
                Next
            Else
                For Each opt In _pickOneOptions
                    opt.IsSelected = False
                Next
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

        Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
            ConfirmSelection()
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            CancelSelection()
        End Sub

        Private Sub ConfirmSelection()
            HideError()

            If _mode = ServiceConsumablesSelectionMode.Fixed Then
                Dim selected = _fixedOptions.Where(Function(o) o.IsSelected).ToList()
                For Each opt In selected
                    If opt.Quantity <= 0D Then
                        ShowError("Each always-deduct product must use a quantity greater than zero.")
                        Return
                    End If
                Next

                ResultFixedOptions = _fixedOptions.Select(Function(o) New FixedConsumableOption With {
                    .Sku = o.Sku,
                    .Name = o.Name,
                    .IsSelected = o.IsSelected,
                    .Quantity = o.Quantity
                }).ToList()
            Else
                Dim selected = _pickOneOptions.Where(Function(o) o.IsSelected).Select(Function(o) o.Sku).ToList()
                If selected.Count > 0 Then
                    Dim defaultQty As Decimal
                    If Not Decimal.TryParse(DefaultQtyBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, defaultQty) OrElse defaultQty <= 0D Then
                        ShowError("Default quantity must be greater than zero.")
                        DefaultQtyBox.Focus()
                        Return
                    End If
                    If selected.Count < 2 Then
                        ShowError("Select at least two products for pick-at-POS, or use Always deduct for a single product.")
                        Return
                    End If
                    ResultPickOneDefaultQty = defaultQty
                End If

                ResultPickOneOptions = _pickOneOptions.Select(Function(o) New PickOneProductOption With {
                    .Sku = o.Sku,
                    .Name = o.Name,
                    .IsSelected = o.IsSelected
                }).ToList()
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
