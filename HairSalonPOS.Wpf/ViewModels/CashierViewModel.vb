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

        Private Const SeniorPromoCode As String = "SENIOR"
        Private Const SeniorMinimumAge As Integer = 60
        Private Const MaximumReasonableAge As Integer = 120

        Private _customerName As String = "Walk-in"
        Private _selectedStylist As StaffMember
        Private _customerBirthDate As Date?
        Private _seniorEligibilityText As String = "Enter birthdate to check senior discount"
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
        Private _selectedCategory As String = String.Empty
        Private _selectedSubCategory As String = String.Empty
        Private _isCatalogEditMode As Boolean
        Private _isAddingCatalog As Boolean = True
        Private _editingSku As String = String.Empty
        Private _editCatalogName As String = String.Empty
        Private _editCatalogPrice As Decimal
        Private _editCatalogType As String = "Service"
        Private _lastReceipt As ReceiptModel

        Public Sub New()
            CatalogTiles = New ObservableCollection(Of CatalogTile)()
            Cart = New ObservableCollection(Of CartLine)()
            CustomerNames = New ObservableCollection(Of String)(_store.Customers.Select(Function(c) c.Name))
            Stylists = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            Categories = New ObservableCollection(Of CatalogCategoryNode)(BuildCategoryTree())
            CategoryChips = New ObservableCollection(Of SelectableChip)(Categories.Select(Function(c) New SelectableChip With {.Name = c.Name}))
            SubCategoryChips = New ObservableCollection(Of SelectableChip)()
            CatalogTypes = New ObservableCollection(Of String) From {"Service", "Product"}

            AddTileCommand = New RelayCommand(Of CatalogTile)(AddressOf AddFromTile)
            RemoveLineCommand = New RelayCommand(Of CartLine)(AddressOf RemoveLine)
            ClearCartCommand = New RelayCommand(AddressOf ClearCart, Function() Cart.Count > 0)
            CheckoutCommand = New RelayCommand(AddressOf ExecuteCheckout, Function() CanCheckout())
            SelectCategoryCommand = New RelayCommand(Of String)(AddressOf SelectCategory)
            SelectSubCategoryCommand = New RelayCommand(Of String)(AddressOf SelectSubCategory)
            BeginAddCatalogCommand = New RelayCommand(AddressOf BeginAddCatalog, AddressOf CanManageCatalog)
            EditCatalogTileCommand = New RelayCommand(Of CatalogTile)(AddressOf BeginEditCatalog, AddressOf CanManageCatalogTile)
            DeleteCatalogTileCommand = New RelayCommand(Of CatalogTile)(AddressOf DeleteCatalogTile, AddressOf CanManageCatalogTile)
            SaveCatalogCommand = New RelayCommand(AddressOf SaveCatalogItem, AddressOf CanManageCatalog)
            CancelCatalogEditCommand = New RelayCommand(Sub() IsCatalogEditMode = False)
            SelectCashCommand = New RelayCommand(Sub() PaymentMethod = "Cash")
            SelectGcashCommand = New RelayCommand(Sub() PaymentMethod = "GCash")
            ApplyPromoCommand = New RelayCommand(AddressOf ApplyPromo)
            ReprintLastReceiptCommand = New RelayCommand(AddressOf ReprintLastReceipt, Function() LastReceipt IsNot Nothing)

            SelectedStylist = Stylists.FirstOrDefault()
            SelectCategory(Categories.First().Name)

            AddHandler _store.StaffChanged, Sub() RefreshStylists()
            AddHandler _store.CustomersChanged, Sub() RefreshCustomerNames()
        End Sub

        Public Sub RefreshLookups()
            RefreshStylists()
            RefreshCustomerNames()
            OnPropertyChanged(NameOf(CanManageCatalogItems))
            BeginAddCatalogCommand.NotifyCanExecuteChanged()
            EditCatalogTileCommand.NotifyCanExecuteChanged()
            DeleteCatalogTileCommand.NotifyCanExecuteChanged()
            SaveCatalogCommand.NotifyCanExecuteChanged()
        End Sub

        ''' <summary>Admin-only: show catalog Add/Edit/Delete controls.</summary>
        Public ReadOnly Property CanManageCatalogItems As Boolean
            Get
                Return SessionContext.IsAdmin
            End Get
        End Property

        Private Sub RefreshStylists()
            Dim selectedId = If(SelectedStylist?.StaffId, 0)
            Stylists = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            OnPropertyChanged(NameOf(Stylists))
            SelectedStylist = Stylists.FirstOrDefault(Function(s) s.StaffId = selectedId)
            If SelectedStylist Is Nothing Then SelectedStylist = Stylists.FirstOrDefault()
        End Sub

        Private Sub RefreshCustomerNames()
            Dim current = CustomerName
            CustomerNames = New ObservableCollection(Of String)(_store.Customers.Select(Function(c) c.Name))
            OnPropertyChanged(NameOf(CustomerNames))
            If Not String.IsNullOrWhiteSpace(current) AndAlso CustomerNames.Contains(current) Then
                CustomerName = current
            End If
        End Sub

        Public Shared Function BuildCategoryTree() As List(Of CatalogCategoryNode)
            Return New List(Of CatalogCategoryNode) From {
                New CatalogCategoryNode With {.Name = "HAIR SERVICES", .SubCategories = New List(Of String) From {"Rebond Packages", "Hair Treatment Packages", "Cut and Styles", "Hair Color", "Hair Treatment"}},
                New CatalogCategoryNode With {.Name = "NAIL SERVICES", .SubCategories = New List(Of String) From {"Basic Care", "Gel and Extensions"}},
                New CatalogCategoryNode With {.Name = "BODY SERVICES", .SubCategories = New List(Of String) From {"Spa and Scrub Packages", "Paraffin Therapy and Massage"}},
                New CatalogCategoryNode With {.Name = "EYELASH SERVICES"},
                New CatalogCategoryNode With {.Name = "EYEBROW SERVICES"},
                New CatalogCategoryNode With {.Name = "WAXING SERVICES"}
            }
        End Function

        Public Property CatalogTiles As ObservableCollection(Of CatalogTile)
        Public Property Cart As ObservableCollection(Of CartLine)
        Public Property CustomerNames As ObservableCollection(Of String)
        Public Property Stylists As ObservableCollection(Of StaffMember)
        Public Property Categories As ObservableCollection(Of CatalogCategoryNode)
        Public Property CategoryChips As ObservableCollection(Of SelectableChip)
        Public Property SubCategoryChips As ObservableCollection(Of SelectableChip)
        Public Property CatalogTypes As ObservableCollection(Of String)

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

        Public Property CustomerBirthDate As Date?
            Get
                Return _customerBirthDate
            End Get
            Set(value As Date?)
                If SetProperty(_customerBirthDate, value) Then
                    ApplySeniorAgeTrapping()
                End If
            End Set
        End Property

        Public Property SeniorEligibilityText As String
            Get
                Return _seniorEligibilityText
            End Get
            Private Set(value As String)
                SetProperty(_seniorEligibilityText, value)
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
                CheckoutCommand.NotifyCanExecuteChanged()
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

        Public Property SelectedCategory As String
            Get
                Return _selectedCategory
            End Get
            Set(value As String)
                SetProperty(_selectedCategory, value)
                OnPropertyChanged(NameOf(HasSubCategories))
                OnPropertyChanged(NameOf(CatalogLeafLabel))
                BeginAddCatalogCommand.NotifyCanExecuteChanged()
                EditCatalogTileCommand.NotifyCanExecuteChanged()
                DeleteCatalogTileCommand.NotifyCanExecuteChanged()
                SaveCatalogCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property SelectedSubCategory As String
            Get
                Return _selectedSubCategory
            End Get
            Set(value As String)
                SetProperty(_selectedSubCategory, value)
                OnPropertyChanged(NameOf(CatalogLeafLabel))
                BeginAddCatalogCommand.NotifyCanExecuteChanged()
                EditCatalogTileCommand.NotifyCanExecuteChanged()
                DeleteCatalogTileCommand.NotifyCanExecuteChanged()
                SaveCatalogCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property HasSubCategories As Boolean
            Get
                Return SubCategoryChips.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasCatalogItems As Boolean
            Get
                Return CatalogTiles.Count > 0
            End Get
        End Property

        Public ReadOnly Property ShowEmptyCatalogMessage As Boolean
            Get
                Return CatalogTiles.Count = 0
            End Get
        End Property

        Public ReadOnly Property CatalogLeafLabel As String
            Get
                If HasSubCategories AndAlso Not String.IsNullOrWhiteSpace(SelectedSubCategory) Then
                    Return SelectedSubCategory
                End If
                Return SelectedCategory
            End Get
        End Property

        Public Property IsCatalogEditMode As Boolean
            Get
                Return _isCatalogEditMode
            End Get
            Set(value As Boolean)
                SetProperty(_isCatalogEditMode, value)
            End Set
        End Property

        Public ReadOnly Property CatalogFormTitle As String
            Get
                Return If(_isAddingCatalog, "Add item", "Edit item")
            End Get
        End Property

        Public ReadOnly Property CanChangeCatalogType As Boolean
            Get
                Return _isAddingCatalog
            End Get
        End Property

        Public Property EditCatalogName As String
            Get
                Return _editCatalogName
            End Get
            Set(value As String)
                SetProperty(_editCatalogName, value)
            End Set
        End Property

        Public Property EditCatalogPrice As Decimal
            Get
                Return _editCatalogPrice
            End Get
            Set(value As Decimal)
                SetProperty(_editCatalogPrice, value)
            End Set
        End Property

        Public Property EditCatalogType As String
            Get
                Return _editCatalogType
            End Get
            Set(value As String)
                SetProperty(_editCatalogType, value)
            End Set
        End Property

        Public Property LastReceipt As ReceiptModel
            Get
                Return _lastReceipt
            End Get
            Private Set(value As ReceiptModel)
                SetProperty(_lastReceipt, value)
                ReprintLastReceiptCommand.NotifyCanExecuteChanged()
            End Set
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

        Public Property AddTileCommand As RelayCommand(Of CatalogTile)
        Public Property RemoveLineCommand As RelayCommand(Of CartLine)
        Public Property ClearCartCommand As RelayCommand
        Public Property CheckoutCommand As RelayCommand
        Public Property SelectCategoryCommand As RelayCommand(Of String)
        Public Property SelectSubCategoryCommand As RelayCommand(Of String)
        Public Property BeginAddCatalogCommand As RelayCommand
        Public Property EditCatalogTileCommand As RelayCommand(Of CatalogTile)
        Public Property DeleteCatalogTileCommand As RelayCommand(Of CatalogTile)
        Public Property SaveCatalogCommand As RelayCommand
        Public Property CancelCatalogEditCommand As RelayCommand
        Public Property SelectCashCommand As RelayCommand
        Public Property SelectGcashCommand As RelayCommand
        Public Property ApplyPromoCommand As RelayCommand
        Public Property ReprintLastReceiptCommand As RelayCommand

        Private Sub SelectCategory(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return
            SelectedCategory = name
            For Each chip In CategoryChips
                chip.IsSelected = String.Equals(chip.Name, name, StringComparison.OrdinalIgnoreCase)
            Next

            Dim node = Categories.FirstOrDefault(Function(c) c.Name = name)
            Dim subs = If(node?.SubCategories, New List(Of String)())
            SubCategoryChips = New ObservableCollection(Of SelectableChip)(subs.Select(Function(s) New SelectableChip With {.Name = s}))
            OnPropertyChanged(NameOf(SubCategoryChips))
            OnPropertyChanged(NameOf(HasSubCategories))

            If SubCategoryChips.Count > 0 Then
                SelectSubCategory(SubCategoryChips.First().Name)
            Else
                SelectedSubCategory = String.Empty
                LoadCatalogTiles()
            End If
        End Sub

        Private Sub SelectSubCategory(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return
            SelectedSubCategory = name
            For Each chip In SubCategoryChips
                chip.IsSelected = String.Equals(chip.Name, name, StringComparison.OrdinalIgnoreCase)
            Next
            LoadCatalogTiles()
        End Sub

        Private Function CanManageCatalog() As Boolean
            If Not SessionContext.IsAdmin Then Return False
            If String.IsNullOrWhiteSpace(SelectedCategory) Then Return False
            If HasSubCategories AndAlso String.IsNullOrWhiteSpace(SelectedSubCategory) Then Return False
            Return True
        End Function

        Private Function CanManageCatalogTile(tile As CatalogTile) As Boolean
            Return CanManageCatalog() AndAlso tile IsNot Nothing
        End Function

        Private Sub RequireCatalogAdmin()
            If Not SessionContext.IsAdmin Then
                Throw New UnauthorizedAccessException("Only Admin can manage catalog items.")
            End If
        End Sub

        Private Function CurrentSubCategoryValue() As String
            Return If(HasSubCategories, SelectedSubCategory, String.Empty)
        End Function

        Private Sub LoadCatalogTiles()
            CatalogTiles.Clear()
            Dim cat = SelectedCategory
            Dim subCat = CurrentSubCategoryValue()

            For Each s In _store.Services.Where(Function(x) MatchesLeaf(x.Category, x.SubCategory, cat, subCat))
                CatalogTiles.Add(New CatalogTile With {
                    .Sku = s.Sku, .Name = s.Name, .Price = s.Price, .Icon = s.Icon,
                    .TileType = "Service", .Category = s.Category, .SubCategory = s.SubCategory
                })
            Next
            For Each p In _store.Products.Where(Function(x) MatchesLeaf(x.Category, x.SubCategory, cat, subCat))
                CatalogTiles.Add(New CatalogTile With {
                    .Sku = p.Sku, .Name = p.Name, .Price = p.Price, .Icon = "🧴",
                    .TileType = "Product", .Category = p.Category, .SubCategory = p.SubCategory
                })
            Next
            OnPropertyChanged(NameOf(HasCatalogItems))
            OnPropertyChanged(NameOf(ShowEmptyCatalogMessage))
        End Sub

        Private Shared Function MatchesLeaf(itemCat As String, itemSub As String, cat As String, subCat As String) As Boolean
            If Not String.Equals(itemCat, cat, StringComparison.OrdinalIgnoreCase) Then Return False
            If String.IsNullOrWhiteSpace(subCat) Then
                Return String.IsNullOrWhiteSpace(itemSub)
            End If
            Return String.Equals(itemSub, subCat, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Sub BeginAddCatalog()
            If Not CanManageCatalog() Then Return
            _isAddingCatalog = True
            _editingSku = String.Empty
            EditCatalogName = String.Empty
            EditCatalogPrice = 0D
            EditCatalogType = "Service"
            OnPropertyChanged(NameOf(CatalogFormTitle))
            OnPropertyChanged(NameOf(CanChangeCatalogType))
            IsCatalogEditMode = True
        End Sub

        Private Sub BeginEditCatalog(tile As CatalogTile)
            If tile Is Nothing OrElse Not CanManageCatalog() Then Return
            _isAddingCatalog = False
            _editingSku = tile.Sku
            EditCatalogName = tile.Name
            EditCatalogPrice = tile.Price
            EditCatalogType = tile.TileType
            OnPropertyChanged(NameOf(CatalogFormTitle))
            OnPropertyChanged(NameOf(CanChangeCatalogType))
            IsCatalogEditMode = True
        End Sub

        Private Sub SaveCatalogItem()
            Try
                RequireCatalogAdmin()
            Catch ex As UnauthorizedAccessException
                StatusMessage = ex.Message
                Return
            End Try
            If String.IsNullOrWhiteSpace(EditCatalogName) Then
                StatusMessage = "Item name is required."
                Return
            End If
            If EditCatalogPrice < 0D Then
                StatusMessage = "Price must be zero or greater."
                Return
            End If

            Dim cat = SelectedCategory
            Dim subCat = CurrentSubCategoryValue()

            If _isAddingCatalog Then
                If EditCatalogType = "Product" Then
                    Dim sku = $"CP{(_store.Products.Count + 1):D3}"
                    _store.Products.Add(New ProductItem With {
                        .Sku = sku,
                        .Name = EditCatalogName.Trim(),
                        .Brand = "Salon",
                        .Price = EditCatalogPrice,
                        .Cost = 0D,
                        .StockOnHand = 100,
                        .ReorderLevel = 5,
                        .Category = cat,
                        .SubCategory = subCat
                    })
                Else
                    Dim sku = $"CS{(_store.Services.Count + 1):D3}"
                    _store.Services.Add(New ServiceItem With {
                        .Sku = sku,
                        .Name = EditCatalogName.Trim(),
                        .Price = EditCatalogPrice,
                        .DurationMinutes = 60,
                        .Icon = "✨",
                        .Category = cat,
                        .SubCategory = subCat
                    })
                End If
                StatusMessage = "Item added."
            Else
                Dim svc = _store.Services.FirstOrDefault(Function(s) s.Sku = _editingSku)
                Dim prod = _store.Products.FirstOrDefault(Function(p) p.Sku = _editingSku)
                If svc IsNot Nothing Then
                    svc.Name = EditCatalogName.Trim()
                    svc.Price = EditCatalogPrice
                    svc.Category = cat
                    svc.SubCategory = subCat
                ElseIf prod IsNot Nothing Then
                    prod.Name = EditCatalogName.Trim()
                    prod.Price = EditCatalogPrice
                    prod.Category = cat
                    prod.SubCategory = subCat
                Else
                    StatusMessage = "Item not found."
                    Return
                End If
                StatusMessage = "Item updated."
            End If

            _store.PersistCatalog()
            IsCatalogEditMode = False
            LoadCatalogTiles()
        End Sub

        Private Sub DeleteCatalogTile(tile As CatalogTile)
            If tile Is Nothing Then Return
            Try
                RequireCatalogAdmin()
            Catch ex As UnauthorizedAccessException
                StatusMessage = ex.Message
                Return
            End Try
            Dim confirm = System.Windows.MessageBox.Show(
                $"Delete '{tile.Name}'?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

            Dim svc = _store.Services.FirstOrDefault(Function(s) s.Sku = tile.Sku)
            If svc IsNot Nothing Then
                _store.Services.Remove(svc)
            Else
                Dim prod = _store.Products.FirstOrDefault(Function(p) p.Sku = tile.Sku)
                If prod IsNot Nothing Then _store.Products.Remove(prod)
            End If
            _store.PersistCatalog()
            StatusMessage = $"{tile.Name} deleted."
            LoadCatalogTiles()
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
            _customerBirthDate = Nothing
            OnPropertyChanged(NameOf(CustomerBirthDate))
            SeniorEligibilityText = "Enter birthdate to check senior discount"
            RecalculateTotals()
            ClearCartCommand.NotifyCanExecuteChanged()
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub ApplyPromo()
            EnforceSeniorPromoEligibility()
            RecalculateTotals()
        End Sub

        Private Sub ApplySeniorAgeTrapping()
            If Not CustomerBirthDate.HasValue Then
                SeniorEligibilityText = "Enter birthdate to check senior discount"
                If IsSeniorPromo(PromoCode) Then
                    PromoCode = String.Empty
                End If
                RecalculateTotals()
                Return
            End If

            Dim birthDate = CustomerBirthDate.Value.Date
            If birthDate > Date.Today Then
                SeniorEligibilityText = "Invalid birthdate — cannot be in the future"
                If IsSeniorPromo(PromoCode) Then
                    PromoCode = String.Empty
                End If
                RecalculateTotals()
                Return
            End If

            Dim age = CalculateAge(birthDate)
            If age > MaximumReasonableAge Then
                SeniorEligibilityText = "Invalid birthdate — age exceeds reasonable limit"
                If IsSeniorPromo(PromoCode) Then
                    PromoCode = String.Empty
                End If
                RecalculateTotals()
                Return
            End If

            If age >= SeniorMinimumAge Then
                PromoCode = SeniorPromoCode
                SeniorEligibilityText = $"Age {age} — Senior discount applied"
            Else
                If IsSeniorPromo(PromoCode) Then
                    PromoCode = String.Empty
                End If
                SeniorEligibilityText = $"Age {age} — Not eligible for senior discount"
            End If

            RecalculateTotals()
        End Sub

        Private Sub EnforceSeniorPromoEligibility()
            If Not IsSeniorPromo(PromoCode) Then Return

            If Not CustomerBirthDate.HasValue Then
                PromoCode = String.Empty
                SeniorEligibilityText = "Birthdate required for senior discount"
                StatusMessage = "Enter customer birthdate to apply senior discount."
                Return
            End If

            Dim birthDate = CustomerBirthDate.Value.Date
            If birthDate > Date.Today Then
                PromoCode = String.Empty
                SeniorEligibilityText = "Invalid birthdate — cannot be in the future"
                StatusMessage = "Senior discount blocked: birthdate cannot be in the future."
                Return
            End If

            Dim age = CalculateAge(birthDate)
            If age > MaximumReasonableAge OrElse age < SeniorMinimumAge Then
                PromoCode = String.Empty
                SeniorEligibilityText = If(age > MaximumReasonableAge,
                    "Invalid birthdate — age exceeds reasonable limit",
                    $"Age {age} — Not eligible for senior discount")
                StatusMessage = "Customer is not eligible for senior discount."
            End If
        End Sub

        Private Function IsSeniorPromo(code As String) As Boolean
            If String.IsNullOrWhiteSpace(code) Then Return False
            If code.Trim().Equals(SeniorPromoCode, StringComparison.OrdinalIgnoreCase) Then Return True
            Dim discount = _store.Discounts.FirstOrDefault(
                Function(d) d.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase))
            Return discount IsNot Nothing AndAlso discount.IsSeniorPwd
        End Function

        Private Shared Function CalculateAge(birthDate As Date) As Integer
            Dim today = Date.Today
            Dim age = today.Year - birthDate.Year
            If birthDate.Date > today.AddYears(-age) Then age -= 1
            Return age
        End Function

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
            If PaymentMethod = "Cash" AndAlso (AmountTendered <= 0D OrElse AmountTendered < Total) Then Return False
            Return True
        End Function

        Private Sub ExecuteCheckout()
            Try
                StatusMessage = String.Empty
                EnforceSeniorPromoEligibility()
                RecalculateTotals()
                If Not CanCheckout() Then
                    If PaymentMethod = "Cash" AndAlso AmountTendered <= 0D Then
                        StatusMessage = "Enter amount tendered before checkout."
                    ElseIf PaymentMethod = "Cash" AndAlso AmountTendered < Total Then
                        StatusMessage = "Amount tendered is less than total."
                    End If
                    Return
                End If
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
    End Class
End Namespace
