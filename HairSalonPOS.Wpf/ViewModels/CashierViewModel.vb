Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class CashierViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _checkout As New CheckoutService()
        Private ReadOnly _print As New ReceiptPrintService()

        Private _customerName As String = "Walk-in"
        Private _selectedStylist As StaffMember
        Private _promoCode As String = String.Empty
        Private _paymentMethod As String = "Cash"
        Private _amountTendered As Decimal
        Private _subTotal As Decimal
        Private _discountAmount As Decimal
        Private _vatableSales As Decimal
        Private _tax As Decimal
        Private _total As Decimal
        Private _changeAmount As Decimal
        Private _statusMessage As String = String.Empty
        Private _catalogTab As String = "Services"
        Private _lastReceipt As ReceiptModel

        Public Sub New()
            CatalogTiles = New ObservableCollection(Of CatalogTile)()
            Cart = New ObservableCollection(Of CartLine)()
            CustomerNames = New ObservableCollection(Of String)(_store.Customers.Select(Function(c) c.Name))
            Stylists = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))

            AddTileCommand = New RelayCommand(Of CatalogTile)(AddressOf AddFromTile)
            RemoveLineCommand = New RelayCommand(Of CartLine)(AddressOf RemoveLine)
            ClearCartCommand = New RelayCommand(AddressOf ClearCart, Function() Cart.Count > 0)
            CheckoutCommand = New RelayCommand(AddressOf ExecuteCheckout, Function() CanCheckout())
            ShowServicesCommand = New RelayCommand(Sub() CatalogTab = "Services")
            ShowProductsCommand = New RelayCommand(Sub() CatalogTab = "Products")
            ShowPackagesCommand = New RelayCommand(Sub() CatalogTab = "Packages")
            SelectCashCommand = New RelayCommand(Sub() PaymentMethod = "Cash")
            SelectGcashCommand = New RelayCommand(Sub() PaymentMethod = "GCash")
            SelectCardCommand = New RelayCommand(Sub() PaymentMethod = "Card")
            ApplyPromoCommand = New RelayCommand(AddressOf RecalculateTotals)
            ReprintLastReceiptCommand = New RelayCommand(AddressOf ReprintLastReceipt, Function() LastReceipt IsNot Nothing)
            EmailReceiptCommand = New RelayCommand(AddressOf EmailLastReceipt, Function() LastReceipt IsNot Nothing)

            SelectedStylist = Stylists.FirstOrDefault()
            LoadCatalogTiles()
        End Sub

        Public Property CatalogTiles As ObservableCollection(Of CatalogTile)
        Public Property Cart As ObservableCollection(Of CartLine)
        Public Property CustomerNames As ObservableCollection(Of String)
        Public Property Stylists As ObservableCollection(Of StaffMember)

        Public Property CustomerName As String
            Get
                Return _customerName
            End Get
            Set(value As String)
                SetProperty(_customerName, value)
            End Set
        End Property

        Public Property SelectedStylist As StaffMember
            Get
                Return _selectedStylist
            End Get
            Set(value As StaffMember)
                SetProperty(_selectedStylist, value)
            End Set
        End Property

        Public Property PromoCode As String
            Get
                Return _promoCode
            End Get
            Set(value As String)
                SetProperty(_promoCode, value)
            End Set
        End Property

        Public Property PaymentMethod As String
            Get
                Return _paymentMethod
            End Get
            Set(value As String)
                SetProperty(_paymentMethod, value)
                OnPropertyChanged(NameOf(ShowChangeCalculator))
                OnPropertyChanged(NameOf(IsCashSelected))
                OnPropertyChanged(NameOf(IsGcashSelected))
                OnPropertyChanged(NameOf(IsCardSelected))
                RecalculateTotals()
            End Set
        End Property

        Public Property AmountTendered As Decimal
            Get
                Return _amountTendered
            End Get
            Set(value As Decimal)
                SetProperty(_amountTendered, value)
                ChangeAmount = Math.Max(0D, AmountTendered - Total)
            End Set
        End Property

        Public Property SubTotal As Decimal
            Get
                Return _subTotal
            End Get
            Private Set(value As Decimal)
                SetProperty(_subTotal, value)
            End Set
        End Property

        Public Property DiscountAmount As Decimal
            Get
                Return _discountAmount
            End Get
            Private Set(value As Decimal)
                SetProperty(_discountAmount, value)
            End Set
        End Property

        Public Property VatableSales As Decimal
            Get
                Return _vatableSales
            End Get
            Private Set(value As Decimal)
                SetProperty(_vatableSales, value)
            End Set
        End Property

        Public Property Tax As Decimal
            Get
                Return _tax
            End Get
            Private Set(value As Decimal)
                SetProperty(_tax, value)
            End Set
        End Property

        Public Property Total As Decimal
            Get
                Return _total
            End Get
            Private Set(value As Decimal)
                SetProperty(_total, value)
                ChangeAmount = Math.Max(0D, AmountTendered - Total)
            End Set
        End Property

        Public Property ChangeAmount As Decimal
            Get
                Return _changeAmount
            End Get
            Private Set(value As Decimal)
                SetProperty(_changeAmount, value)
            End Set
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property CatalogTab As String
            Get
                Return _catalogTab
            End Get
            Set(value As String)
                SetProperty(_catalogTab, value)
                LoadCatalogTiles()
                OnPropertyChanged(NameOf(IsServicesTab))
                OnPropertyChanged(NameOf(IsProductsTab))
                OnPropertyChanged(NameOf(IsPackagesTab))
            End Set
        End Property

        Public Property LastReceipt As ReceiptModel
            Get
                Return _lastReceipt
            End Get
            Private Set(value As ReceiptModel)
                SetProperty(_lastReceipt, value)
                ReprintLastReceiptCommand.NotifyCanExecuteChanged()
                EmailReceiptCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property IsServicesTab As Boolean
            Get
                Return CatalogTab = "Services"
            End Get
        End Property

        Public ReadOnly Property IsProductsTab As Boolean
            Get
                Return CatalogTab = "Products"
            End Get
        End Property

        Public ReadOnly Property IsPackagesTab As Boolean
            Get
                Return CatalogTab = "Packages"
            End Get
        End Property

        Public ReadOnly Property ShowChangeCalculator As Boolean
            Get
                Return PaymentMethod = "Cash"
            End Get
        End Property

        Public ReadOnly Property IsCashSelected As Boolean
            Get
                Return PaymentMethod = "Cash"
            End Get
        End Property

        Public ReadOnly Property IsGcashSelected As Boolean
            Get
                Return PaymentMethod = "GCash"
            End Get
        End Property

        Public ReadOnly Property IsCardSelected As Boolean
            Get
                Return PaymentMethod = "Card"
            End Get
        End Property

        Public Property AddTileCommand As RelayCommand(Of CatalogTile)
        Public Property RemoveLineCommand As RelayCommand(Of CartLine)
        Public Property ClearCartCommand As RelayCommand
        Public Property CheckoutCommand As RelayCommand
        Public Property ShowServicesCommand As RelayCommand
        Public Property ShowProductsCommand As RelayCommand
        Public Property ShowPackagesCommand As RelayCommand
        Public Property SelectCashCommand As RelayCommand
        Public Property SelectGcashCommand As RelayCommand
        Public Property SelectCardCommand As RelayCommand
        Public Property ApplyPromoCommand As RelayCommand
        Public Property ReprintLastReceiptCommand As RelayCommand
        Public Property EmailReceiptCommand As RelayCommand

        Private Sub LoadCatalogTiles()
            CatalogTiles.Clear()
            Select Case CatalogTab
                Case "Services"
                    For Each s In _store.Services
                        CatalogTiles.Add(New CatalogTile With {.Sku = s.Sku, .Name = s.Name, .Price = s.Price, .Icon = s.Icon, .TileType = "Service"})
                    Next
                Case "Products"
                    For Each p In _store.Products
                        CatalogTiles.Add(New CatalogTile With {.Sku = p.Sku, .Name = p.Name, .Price = p.Price, .Icon = "🧴", .TileType = "Product"})
                    Next
                Case "Packages"
                    For Each pkg In _store.Packages
                        CatalogTiles.Add(New CatalogTile With {.Sku = pkg.Sku, .Name = pkg.Name, .Price = pkg.Price, .Icon = pkg.Icon, .TileType = "Package"})
                    Next
            End Select
        End Sub

        Private Sub AddFromTile(tile As CatalogTile)
            If tile Is Nothing Then Return
            Select Case tile.TileType
                Case "Service"
                    AddToCart(tile.Sku, tile.Name, tile.Price, True)
                Case "Product"
                    Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku = tile.Sku)
                    If product Is Nothing OrElse product.StockOnHand <= 0 Then
                        StatusMessage = $"{tile.Name} is out of stock."
                        Return
                    End If
                    AddToCart(tile.Sku, tile.Name, tile.Price, False)
                Case "Package"
                    Dim pkg = _store.Packages.First(Function(p) p.Sku = tile.Sku)
                    For Each sku In pkg.IncludedSkus
                        Dim svc = _store.Services.FirstOrDefault(Function(s) s.Sku = sku)
                        If svc IsNot Nothing Then AddToCart(svc.Sku, svc.Name, svc.Price, True)
                    Next
                    StatusMessage = $"{pkg.Name} added to cart."
            End Select
        End Sub

        Private Sub AddToCart(sku As String, name As String, price As Decimal, isService As Boolean)
            Dim existing = Cart.FirstOrDefault(Function(c) c.Sku = sku)
            If existing IsNot Nothing Then
                If Not isService Then
                    Dim product = _store.Products.First(Function(p) p.Sku = sku)
                    If existing.Quantity >= product.StockOnHand Then
                        StatusMessage = "Not enough stock available."
                        Return
                    End If
                End If
                existing.Quantity += 1
            Else
                Cart.Add(New CartLine With {.Sku = sku, .Name = name, .UnitPrice = price, .Quantity = 1, .IsService = isService})
            End If
            StatusMessage = String.Empty
            RecalculateTotals()
            ClearCartCommand.NotifyCanExecuteChanged()
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub RemoveLine(line As CartLine)
            Cart.Remove(line)
            RecalculateTotals()
            ClearCartCommand.NotifyCanExecuteChanged()
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub ClearCart()
            Cart.Clear()
            PromoCode = String.Empty
            AmountTendered = 0D
            StatusMessage = String.Empty
            RecalculateTotals()
            ClearCartCommand.NotifyCanExecuteChanged()
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub RecalculateTotals()
            SubTotal = Cart.Sum(Function(c) c.LineTotal)
            DiscountAmount = 0D
            If Not String.IsNullOrWhiteSpace(PromoCode) Then
                Try
                    DiscountAmount = _store.ApplyDiscount(SubTotal, PromoCode)
                Catch ex As Exception
                    StatusMessage = ex.Message
                End Try
            End If
            Dim taxable = Math.Max(0D, SubTotal - DiscountAmount)
            Tax = Math.Round(taxable - (taxable / (1D + InMemoryDataStore.TaxRate)), 2)
            VatableSales = taxable - Tax
            Total = taxable
            ChangeAmount = Math.Max(0D, AmountTendered - Total)
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Function CanCheckout() As Boolean
            If Cart.Count = 0 Then Return False
            If PaymentMethod = "Cash" AndAlso AmountTendered > 0 AndAlso AmountTendered < Total Then Return False
            Return True
        End Function

        Private Sub ExecuteCheckout()
            Try
                StatusMessage = String.Empty
                Dim request As New CheckoutRequest With {
                    .Cart = Cart.ToList(),
                    .PaymentMethod = PaymentMethod,
                    .CashierName = SessionContext.CurrentUser.FullName,
                    .CustomerName = CustomerName,
                    .StylistName = If(SelectedStylist?.Name, String.Empty),
                    .PromoCode = PromoCode,
                    .AmountTendered = AmountTendered
                }
                LastReceipt = _checkout.FinalizeSale(request)
                Try
                    _print.PrintReceipt(LastReceipt, showDialog:=True)
                    StatusMessage = $"Sale {LastReceipt.ReceiptNumber} completed and sent to printer."
                Catch printEx As Exception
                    StatusMessage = $"Sale {LastReceipt.ReceiptNumber} saved, but printing failed: {printEx.Message}"
                End Try
                ClearCart()
            Catch ex As Exception
                StatusMessage = ex.Message
            End Try
        End Sub

        Private Sub ReprintLastReceipt()
            If LastReceipt Is Nothing Then Return
            Try
                _print.PrintReceipt(LastReceipt, showDialog:=True)
                StatusMessage = $"Reprinted {LastReceipt.ReceiptNumber}."
            Catch ex As Exception
                StatusMessage = $"Reprint failed: {ex.Message}"
            End Try
        End Sub

        Private Sub EmailLastReceipt()
            If LastReceipt Is Nothing Then Return
            System.Windows.MessageBox.Show($"Receipt {LastReceipt.ReceiptNumber} would be sent to customer email (demo).", "Send Receipt")
        End Sub
    End Class
End Namespace
