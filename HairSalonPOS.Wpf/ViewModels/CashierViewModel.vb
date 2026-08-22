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
        Private Const DefaultCustomerName As String = "Walk-in"
        Private Const UnsavedCategoryReminder As String = "Click Save categories to update the POS tabs."

        Private _customerName As String = DefaultCustomerName
        Private _selectedStylist As StaffMember
        Private _customerBirthDate As Date?
        Private _seniorEligibilityText As String = String.Empty
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
        Private _isCategoryManageMode As Boolean
        Private _isAddingCatalog As Boolean = True
        Private _editingSku As String = String.Empty
        Private _editCatalogName As String = String.Empty
        Private _editCatalogPrice As Decimal
        Private _editCatalogType As String = "Service"
        Private _lastReceipt As ReceiptModel
        Private _selectedManageCategory As CatalogCategoryNode
        Private _selectedManageSubCategory As String
        Private _editCategoryName As String = String.Empty
        Private _editSubCategoryName As String = String.Empty
        Private _hasUnsavedCategoryChanges As Boolean
        Private _lastFocusedCategoryName As String = String.Empty
        Private _lastFocusedSubCategoryName As String = String.Empty
        Private _pendingAppointmentId As Integer

        Public Sub New()
            CatalogTiles = New ObservableCollection(Of CatalogTile)()
            Cart = New ObservableCollection(Of CartLine)()
            Stylists = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            Categories = New ObservableCollection(Of CatalogCategoryNode)(_store.Categories.Where(Function(c) c.IsActive))
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
            BeginManageCategoriesCommand = New RelayCommand(AddressOf BeginManageCategories, Function() SessionContext.IsAdmin)
            AddCategoryCommand = New RelayCommand(AddressOf AddCategory, Function() IsCategoryManageMode)
            AddSubCategoryCommand = New RelayCommand(AddressOf AddSubCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing)
            RenameCategoryCommand = New RelayCommand(AddressOf RenameCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing)
            RenameSubCategoryCommand = New RelayCommand(AddressOf RenameSubCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(SelectedManageSubCategory))
            DeleteCategoryCommand = New RelayCommand(AddressOf DeleteCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing)
            DeleteSubCategoryCommand = New RelayCommand(AddressOf DeleteSubCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(SelectedManageSubCategory))
            SaveCategoriesCommand = New RelayCommand(AddressOf SaveCategories, Function() IsCategoryManageMode)
            CancelCategoryManageCommand = New RelayCommand(AddressOf CancelCategoryManage, Function() IsCategoryManageMode)
            SelectCashCommand = New RelayCommand(Sub() PaymentMethod = "Cash")
            SelectGcashCommand = New RelayCommand(Sub() PaymentMethod = "GCash")
            ApplyPromoCommand = New RelayCommand(AddressOf ApplyPromo)
            ReprintLastReceiptCommand = New RelayCommand(AddressOf ReprintLastReceipt, Function() LastReceipt IsNot Nothing)

            SelectedStylist = Stylists.FirstOrDefault()
            If Categories.Count > 0 Then SelectCategory(Categories.First().Name)

            AddHandler _store.StaffChanged, Sub() RefreshStylists()
        End Sub

        Public Sub RefreshLookups()
            RefreshStylists()
            RefreshCategoriesFromStore()
            OnPropertyChanged(NameOf(CanManageCatalogItems))
            BeginAddCatalogCommand.NotifyCanExecuteChanged()
            EditCatalogTileCommand.NotifyCanExecuteChanged()
            DeleteCatalogTileCommand.NotifyCanExecuteChanged()
            SaveCatalogCommand.NotifyCanExecuteChanged()
            BeginManageCategoriesCommand.NotifyCanExecuteChanged()
        End Sub

        Public Sub LoadFromAppointment(appt As AppointmentItem)
            If appt Is Nothing Then Return

            Cart.Clear()
            PromoCode = String.Empty
            AmountTendered = 0D
            _customerBirthDate = Nothing
            OnPropertyChanged(NameOf(CustomerBirthDate))
            SeniorEligibilityText = String.Empty

            _pendingAppointmentId = appt.AppointmentId
            CustomerName = If(String.IsNullOrWhiteSpace(appt.CustomerName), DefaultCustomerName, appt.CustomerName.Trim())

            If Not String.IsNullOrWhiteSpace(appt.StaffName) Then
                Dim stylist = Stylists.FirstOrDefault(Function(s) s.Name.Equals(appt.StaffName, StringComparison.OrdinalIgnoreCase))
                If stylist IsNot Nothing Then SelectedStylist = stylist
            End If

            Dim service = FindServiceByName(appt.ServiceName)
            If service IsNot Nothing Then
                AddToCart(service.Sku, service.Name, service.Price, True)
                StatusMessage = $"Appointment loaded for {CustomerName}."
            Else
                Cart.Add(New CartLine With {
                    .Sku = $"APT{appt.AppointmentId}",
                    .Name = appt.ServiceName,
                    .UnitPrice = 0D,
                    .Quantity = 1,
                    .IsService = True
                })
                RecalculateTotals()
                ClearCartCommand.NotifyCanExecuteChanged()
                CheckoutCommand.NotifyCanExecuteChanged()
                StatusMessage = $"Service ""{appt.ServiceName}"" was not found in the catalog — added with ₱0. Set the price before checkout."
            End If
        End Sub

        Private Function FindServiceByName(serviceName As String) As ServiceItem
            If String.IsNullOrWhiteSpace(serviceName) Then Return Nothing

            Dim exact = _store.Services.FirstOrDefault(Function(s) s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
            If exact IsNot Nothing Then Return exact

            Return _store.Services.FirstOrDefault(
                Function(s) s.Name.IndexOf(serviceName, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                            serviceName.IndexOf(s.Name, StringComparison.OrdinalIgnoreCase) >= 0)
        End Function

        Public ReadOnly Property CanManageCatalogItems As Boolean
            Get
                Return SessionContext.IsAdmin
            End Get
        End Property

        Public ReadOnly Property IsBrowseMode As Boolean
            Get
                Return Not IsCatalogEditMode AndAlso Not IsCategoryManageMode
            End Get
        End Property

        Private Sub RefreshStylists()
            Dim selectedId = If(SelectedStylist?.StaffId, 0)
            Stylists = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            OnPropertyChanged(NameOf(Stylists))
            SelectedStylist = Stylists.FirstOrDefault(Function(s) s.StaffId = selectedId)
            If SelectedStylist Is Nothing Then SelectedStylist = Stylists.FirstOrDefault()
        End Sub

        Private Sub RefreshCategoriesFromStore(Optional selectCategoryName As String = Nothing, Optional selectSubCategoryName As String = Nothing)
            Dim cat = If(Not String.IsNullOrWhiteSpace(selectCategoryName), selectCategoryName, SelectedCategory)
            Dim subCat = If(Not String.IsNullOrWhiteSpace(selectSubCategoryName), selectSubCategoryName, SelectedSubCategory)
            Categories = New ObservableCollection(Of CatalogCategoryNode)(_store.Categories.Where(Function(c) c.IsActive))
            OnPropertyChanged(NameOf(Categories))
            CategoryChips = New ObservableCollection(Of SelectableChip)(Categories.Select(Function(c) New SelectableChip With {.Name = c.Name}))
            OnPropertyChanged(NameOf(CategoryChips))

            If Not String.IsNullOrWhiteSpace(cat) AndAlso Categories.Any(Function(c) c.Name.Equals(cat, StringComparison.OrdinalIgnoreCase)) Then
                SelectCategory(cat)
                If Not String.IsNullOrWhiteSpace(subCat) AndAlso SubCategoryChips.Any(Function(s) s.Name.Equals(subCat, StringComparison.OrdinalIgnoreCase)) Then
                    SelectSubCategory(subCat)
                End If
            ElseIf Categories.Count > 0 Then
                SelectCategory(Categories.First().Name)
            End If
        End Sub

        Public Property CatalogTiles As ObservableCollection(Of CatalogTile)
        Public Property Cart As ObservableCollection(Of CartLine)
        Public Property Stylists As ObservableCollection(Of StaffMember)
        Public Property Categories As ObservableCollection(Of CatalogCategoryNode)
        Public Property CategoryChips As ObservableCollection(Of SelectableChip)
        Public Property SubCategoryChips As ObservableCollection(Of SelectableChip)
        Public Property CatalogTypes As ObservableCollection(Of String)
        Public Property ManageCategories As ObservableCollection(Of CatalogCategoryNode)
        Public Property ManageSubCategories As ObservableCollection(Of String)

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
                SetProperty(_customerBirthDate, value)
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
                If SetProperty(_isCatalogEditMode, value) Then
                    OnPropertyChanged(NameOf(IsBrowseMode))
                End If
            End Set
        End Property

        Public Property IsCategoryManageMode As Boolean
            Get
                Return _isCategoryManageMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isCategoryManageMode, value) Then
                    OnPropertyChanged(NameOf(IsBrowseMode))
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public Property SelectedManageCategory As CatalogCategoryNode
            Get
                Return _selectedManageCategory
            End Get
            Set(value As CatalogCategoryNode)
                If SetProperty(_selectedManageCategory, value) Then
                    RefreshManageSubCategories()
                    EditCategoryName = If(value?.Name, String.Empty)
                    SelectedManageSubCategory = Nothing
                    EditSubCategoryName = String.Empty
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public Property SelectedManageSubCategory As String
            Get
                Return _selectedManageSubCategory
            End Get
            Set(value As String)
                If SetProperty(_selectedManageSubCategory, value) Then
                    EditSubCategoryName = If(value, String.Empty)
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public Property EditCategoryName As String
            Get
                Return _editCategoryName
            End Get
            Set(value As String)
                SetProperty(_editCategoryName, value)
            End Set
        End Property

        Public Property EditSubCategoryName As String
            Get
                Return _editSubCategoryName
            End Get
            Set(value As String)
                SetProperty(_editSubCategoryName, value)
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
        Public Property BeginManageCategoriesCommand As RelayCommand
        Public Property AddCategoryCommand As RelayCommand
        Public Property AddSubCategoryCommand As RelayCommand
        Public Property RenameCategoryCommand As RelayCommand
        Public Property RenameSubCategoryCommand As RelayCommand
        Public Property DeleteCategoryCommand As RelayCommand
        Public Property DeleteSubCategoryCommand As RelayCommand
        Public Property SaveCategoriesCommand As RelayCommand
        Public Property CancelCategoryManageCommand As RelayCommand
        Public Property SelectCashCommand As RelayCommand
        Public Property SelectGcashCommand As RelayCommand
        Public Property ApplyPromoCommand As RelayCommand
        Public Property ReprintLastReceiptCommand As RelayCommand

        Private Sub NotifyCategoryManageCommands()
            AddCategoryCommand.NotifyCanExecuteChanged()
            AddSubCategoryCommand.NotifyCanExecuteChanged()
            RenameCategoryCommand.NotifyCanExecuteChanged()
            RenameSubCategoryCommand.NotifyCanExecuteChanged()
            DeleteCategoryCommand.NotifyCanExecuteChanged()
            DeleteSubCategoryCommand.NotifyCanExecuteChanged()
            SaveCategoriesCommand.NotifyCanExecuteChanged()
            CancelCategoryManageCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub SelectCategory(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return
            Dim node = Categories.FirstOrDefault(Function(c) c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            If node Is Nothing Then Return

            SelectedCategory = node.Name
            For Each chip In CategoryChips
                chip.IsSelected = String.Equals(chip.Name, node.Name, StringComparison.OrdinalIgnoreCase)
            Next

            Dim subs = If(node.SubCategories, New List(Of String)())
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

        Private Sub BeginManageCategories()
            ManageCategories = New ObservableCollection(Of CatalogCategoryNode)(
                _store.Categories.Select(Function(c) CloneCategory(c)))
            OnPropertyChanged(NameOf(ManageCategories))
            SelectedManageCategory = ManageCategories.FirstOrDefault()
            _hasUnsavedCategoryChanges = False
            _lastFocusedCategoryName = String.Empty
            _lastFocusedSubCategoryName = String.Empty
            StatusMessage = String.Empty
            IsCategoryManageMode = True
        End Sub

        Private Sub CancelCategoryManage()
            If _hasUnsavedCategoryChanges Then
                If Not AppDialogService.Confirm(
                    "Discard unsaved category changes?",
                    "Unsaved changes",
                    "Discard",
                    "Cancel",
                    AppDialogType.Confirmation) Then Return
            End If
            CloseCategoryManagePanel()
        End Sub

        Private Sub CloseCategoryManagePanel()
            _hasUnsavedCategoryChanges = False
            IsCategoryManageMode = False
            ManageCategories = Nothing
            SelectedManageCategory = Nothing
            ManageSubCategories = Nothing
            SelectedManageSubCategory = Nothing
            EditCategoryName = String.Empty
            EditSubCategoryName = String.Empty
            OnPropertyChanged(NameOf(ManageCategories))
            OnPropertyChanged(NameOf(ManageSubCategories))
        End Sub

        Private Sub MarkCategoryManageDirty()
            _hasUnsavedCategoryChanges = True
            StatusMessage = UnsavedCategoryReminder
        End Sub

        Private Sub SaveCategories()
            If ManageCategories Is Nothing OrElse ManageCategories.Count = 0 Then
                StatusMessage = "At least one category is required."
                Return
            End If

            Dim duplicate = ManageCategories.GroupBy(Function(c) c.Name.Trim().ToLowerInvariant()).FirstOrDefault(Function(g) g.Count() > 1)
            If duplicate IsNot Nothing Then
                StatusMessage = $"Duplicate category name: {duplicate.First().Name}"
                Return
            End If

            For Each node In ManageCategories
                If String.IsNullOrWhiteSpace(node.Name) Then
                    StatusMessage = "Category name is required."
                    Return
                End If
                Dim dupSub = node.SubCategories.GroupBy(Function(s) s.Trim().ToLowerInvariant()).FirstOrDefault(Function(g) g.Count() > 1)
                If dupSub IsNot Nothing Then
                    StatusMessage = $"Duplicate subcategory in {node.Name}: {dupSub.First()}"
                    Return
                End If
            Next

            _store.Categories.Clear()
            _store.Categories.AddRange(ManageCategories.Select(Function(c) CloneCategory(c)))
            _store.PersistCatalog()

            Dim categoryToSelect = _lastFocusedCategoryName
            Dim subCategoryToSelect = _lastFocusedSubCategoryName
            RefreshCategoriesFromStore(categoryToSelect, subCategoryToSelect)
            CloseCategoryManagePanel()
            StatusMessage = "Categories saved."
        End Sub

        Private Sub AddCategory()
            Dim name = EditCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(name) Then
                StatusMessage = "Enter a category name."
                Return
            End If
            If ManageCategories.Any(Function(c) c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                StatusMessage = "Category name already exists."
                Return
            End If

            Dim node = New CatalogCategoryNode With {.Name = name, .IsActive = True}
            ManageCategories.Add(node)
            SelectedManageCategory = node
            EditCategoryName = name
            _lastFocusedCategoryName = name
            _lastFocusedSubCategoryName = String.Empty
            MarkCategoryManageDirty()
        End Sub

        Private Sub AddSubCategory()
            If SelectedManageCategory Is Nothing Then Return
            Dim name = EditSubCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(name) Then
                StatusMessage = "Enter a subcategory name."
                Return
            End If
            If SelectedManageCategory.SubCategories.Any(Function(s) s.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                StatusMessage = "Subcategory name already exists."
                Return
            End If

            SelectedManageCategory.SubCategories.Add(name)
            RefreshManageSubCategories()
            SelectedManageSubCategory = name
            EditSubCategoryName = name
            _lastFocusedCategoryName = SelectedManageCategory.Name
            _lastFocusedSubCategoryName = name
            MarkCategoryManageDirty()
        End Sub

        Private Sub RenameCategory()
            If SelectedManageCategory Is Nothing Then Return
            Dim newName = EditCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(newName) Then
                StatusMessage = "Category name is required."
                Return
            End If
            If ManageCategories.Any(Function(c) Not Object.ReferenceEquals(c, SelectedManageCategory) AndAlso c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)) Then
                StatusMessage = "Category name already exists."
                Return
            End If

            Dim oldName = SelectedManageCategory.Name
            If oldName.Equals(newName, StringComparison.OrdinalIgnoreCase) Then Return

            SelectedManageCategory.Name = newName
            UpdateCategoryReferences(oldName, newName, Nothing, Nothing)
            For Each chip In CategoryChips.Where(Function(c) c.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                chip.Name = newName
            Next
            If SelectedCategory.Equals(oldName, StringComparison.OrdinalIgnoreCase) Then
                SelectedCategory = newName
            End If
            _lastFocusedCategoryName = newName
            MarkCategoryManageDirty()
        End Sub

        Private Sub RenameSubCategory()
            If SelectedManageCategory Is Nothing OrElse String.IsNullOrWhiteSpace(SelectedManageSubCategory) Then Return
            Dim newName = EditSubCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(newName) Then
                StatusMessage = "Subcategory name is required."
                Return
            End If
            If SelectedManageCategory.SubCategories.Any(Function(s) Not s.Equals(SelectedManageSubCategory, StringComparison.OrdinalIgnoreCase) AndAlso s.Equals(newName, StringComparison.OrdinalIgnoreCase)) Then
                StatusMessage = "Subcategory name already exists."
                Return
            End If

            Dim index = SelectedManageCategory.SubCategories.FindIndex(Function(s) s.Equals(SelectedManageSubCategory, StringComparison.OrdinalIgnoreCase))
            If index < 0 Then Return
            If SelectedManageSubCategory.Equals(newName, StringComparison.OrdinalIgnoreCase) Then Return

            SelectedManageCategory.SubCategories(index) = newName
            UpdateCategoryReferences(SelectedManageCategory.Name, SelectedManageCategory.Name, SelectedManageSubCategory, newName)
            If SelectedSubCategory.Equals(SelectedManageSubCategory, StringComparison.OrdinalIgnoreCase) Then
                SelectedSubCategory = newName
            End If
            RefreshManageSubCategories()
            SelectedManageSubCategory = newName
            _lastFocusedCategoryName = SelectedManageCategory.Name
            _lastFocusedSubCategoryName = newName
            MarkCategoryManageDirty()
        End Sub

        Private Sub DeleteCategory()
            If SelectedManageCategory Is Nothing Then Return
            Dim categoryName = SelectedManageCategory.Name
            Dim serviceCount = _store.Services.Where(Function(s) s.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim productCount = _store.Products.Where(Function(p) p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim itemCount = serviceCount + productCount

            If itemCount > 0 Then
                If Not AppDialogService.ConfirmDelete(categoryName, $"{itemCount} catalog item(s) still use category '{categoryName}'.") Then Return
            Else
                If Not AppDialogService.ConfirmDelete(categoryName) Then Return
            End If

            ManageCategories.Remove(SelectedManageCategory)
            SelectedManageCategory = ManageCategories.FirstOrDefault()
            EditCategoryName = If(SelectedManageCategory?.Name, String.Empty)
            _lastFocusedCategoryName = If(SelectedManageCategory?.Name, String.Empty)
            _lastFocusedSubCategoryName = String.Empty
            MarkCategoryManageDirty()
        End Sub

        Private Sub DeleteSubCategory()
            If SelectedManageCategory Is Nothing OrElse String.IsNullOrWhiteSpace(SelectedManageSubCategory) Then Return
            Dim categoryName = SelectedManageCategory.Name
            Dim subName = SelectedManageSubCategory
            Dim serviceCount = _store.Services.Where(Function(s) s.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase) AndAlso s.SubCategory.Equals(subName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim productCount = _store.Products.Where(Function(p) p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase) AndAlso p.SubCategory.Equals(subName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim itemCount = serviceCount + productCount

            If itemCount > 0 Then
                If Not AppDialogService.ConfirmDelete(subName, $"{itemCount} catalog item(s) still use subcategory '{subName}'.") Then Return
            Else
                If Not AppDialogService.ConfirmDelete(subName) Then Return
            End If

            SelectedManageCategory.SubCategories.RemoveAll(Function(s) s.Equals(subName, StringComparison.OrdinalIgnoreCase))
            RefreshManageSubCategories()
            SelectedManageSubCategory = Nothing
            EditSubCategoryName = String.Empty
            _lastFocusedSubCategoryName = String.Empty
            MarkCategoryManageDirty()
        End Sub

        Private Sub RefreshManageSubCategories()
            Dim subs = If(SelectedManageCategory?.SubCategories, New List(Of String)())
            ManageSubCategories = New ObservableCollection(Of String)(subs)
            OnPropertyChanged(NameOf(ManageSubCategories))
        End Sub

        Private Sub UpdateCategoryReferences(oldCategory As String, newCategory As String, oldSubCategory As String, newSubCategory As String)
            For Each service In _store.Services
                If Not service.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase) Then Continue For
                If oldSubCategory IsNot Nothing AndAlso Not service.SubCategory.Equals(oldSubCategory, StringComparison.OrdinalIgnoreCase) Then Continue For
                service.Category = newCategory
                If newSubCategory IsNot Nothing Then service.SubCategory = newSubCategory
            Next
            For Each product In _store.Products
                If Not product.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase) Then Continue For
                If oldSubCategory IsNot Nothing AndAlso Not product.SubCategory.Equals(oldSubCategory, StringComparison.OrdinalIgnoreCase) Then Continue For
                product.Category = newCategory
                If newSubCategory IsNot Nothing Then product.SubCategory = newSubCategory
            Next
        End Sub

        Private Shared Function CloneCategory(source As CatalogCategoryNode) As CatalogCategoryNode
            Return New CatalogCategoryNode With {
                .Name = source.Name,
                .IsActive = source.IsActive,
                .SubCategories = New List(Of String)(If(source.SubCategories, New List(Of String)()))
            }
        End Function

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

            For Each s In _store.Services.Where(Function(x) x.IsActive AndAlso MatchesLeaf(x.Category, x.SubCategory, cat, subCat))
                CatalogTiles.Add(New CatalogTile With {
                    .Sku = s.Sku, .Name = s.Name, .Price = s.Price, .Icon = s.Icon,
                    .TileType = "Service", .Category = s.Category, .SubCategory = s.SubCategory
                })
            Next
            For Each p In _store.Products.Where(Function(x) x.IsActive AndAlso MatchesLeaf(x.Category, x.SubCategory, cat, subCat))
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
                        .SubCategory = subCat,
                        .IsActive = True
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
                        .SubCategory = subCat,
                        .CommissionPercent = 0D,
                        .IsActive = True
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
            If Not AppDialogService.ConfirmDelete(tile.Name) Then Return

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
            CustomerName = DefaultCustomerName
            _pendingAppointmentId = 0
            StatusMessage = String.Empty
            _customerBirthDate = Nothing
            OnPropertyChanged(NameOf(CustomerBirthDate))
            SeniorEligibilityText = String.Empty
            RecalculateTotals()
            ClearCartCommand.NotifyCanExecuteChanged()
            CheckoutCommand.NotifyCanExecuteChanged()
        End Sub

        Private Shared Function NormalizeCustomerName(name As String) As String
            If String.IsNullOrWhiteSpace(name) Then Return DefaultCustomerName
            Return name.Trim()
        End Function

        Private Sub ApplyPromo()
            If String.IsNullOrWhiteSpace(PromoCode) Then
                AppDialogService.ShowWarning("Enter a promo code.", "Promo code")
                RecalculateTotals()
                Return
            End If

            If IsSeniorPromo(PromoCode) Then
                ApplySeniorPromoWithBirthdatePrompt()
                Return
            End If

            Dim code = PromoCode.Trim()
            Dim discount = _store.Discounts.FirstOrDefault(Function(d) d.Code.Equals(code, StringComparison.OrdinalIgnoreCase) AndAlso d.IsActive)
            If discount Is Nothing Then
                AppDialogService.ShowWarning("Invalid or inactive promo code.", "Promo code")
                StatusMessage = String.Empty
            Else
                StatusMessage = $"Promo {discount.Code} applied."
                AppDialogService.ShowSuccess($"Promo {discount.Code} applied.", "Promo applied")
            End If
            RecalculateTotals()
        End Sub

        Private Sub ApplySeniorPromoWithBirthdatePrompt()
            Dim birth = AppDialogService.PromptBirthdate(_customerBirthDate)
            If Not birth.HasValue Then
                PromoCode = String.Empty
                _customerBirthDate = Nothing
                OnPropertyChanged(NameOf(CustomerBirthDate))
                SeniorEligibilityText = String.Empty
                StatusMessage = String.Empty
                AppDialogService.ShowInfo("Senior discount cancelled.", "Senior discount")
                RecalculateTotals()
                Return
            End If

            Dim validation = ValidateSeniorBirthdate(birth.Value)
            If Not validation.IsEligible Then
                PromoCode = String.Empty
                _customerBirthDate = Nothing
                OnPropertyChanged(NameOf(CustomerBirthDate))
                SeniorEligibilityText = validation.Message
                StatusMessage = String.Empty
                AppDialogService.ShowWarning(validation.Message, "Not eligible")
                RecalculateTotals()
                Return
            End If

            _customerBirthDate = birth.Value.Date
            OnPropertyChanged(NameOf(CustomerBirthDate))
            PromoCode = SeniorPromoCode
            SeniorEligibilityText = validation.Message
            StatusMessage = validation.Message
            AppDialogService.ShowSuccess(validation.Message, "Senior discount")
            RecalculateTotals()
        End Sub

        Private Function ValidateSeniorBirthdate(birthDate As Date) As (IsEligible As Boolean, Message As String)
            Dim dateOnly = birthDate.Date
            If dateOnly > Date.Today Then
                Return (False, "Invalid birthdate — cannot be in the future")
            End If

            Dim age = CalculateAge(dateOnly)
            If age > MaximumReasonableAge Then
                Return (False, "Invalid birthdate — age exceeds reasonable limit")
            End If

            If age < SeniorMinimumAge Then
                Return (False, $"Age {age} — Not eligible for senior discount")
            End If

            Return (True, $"Age {age} — Senior discount applied")
        End Function

        Private Sub EnforceSeniorPromoEligibility()
            If Not IsSeniorPromo(PromoCode) Then Return

            If Not CustomerBirthDate.HasValue Then
                PromoCode = String.Empty
                SeniorEligibilityText = String.Empty
                StatusMessage = "Senior discount requires birthdate verification. Click Apply after entering SENIOR."
                Return
            End If

            Dim validation = ValidateSeniorBirthdate(CustomerBirthDate.Value)
            If Not validation.IsEligible Then
                PromoCode = String.Empty
                SeniorEligibilityText = validation.Message
                StatusMessage = validation.Message
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
            Tax = 0D
            VatableSales = 0D
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
                    .CustomerName = NormalizeCustomerName(CustomerName),
                    .StylistName = If(SelectedStylist?.Name, String.Empty),
                    .PromoCode = PromoCode,
                    .AmountTendered = AmountTendered
                }
                LastReceipt = _checkout.FinalizeSale(request)
                If _pendingAppointmentId > 0 Then
                    _store.MarkAppointmentDone(_pendingAppointmentId)
                    _pendingAppointmentId = 0
                End If
                Dim preview As New Views.ReceiptPreviewWindow(LastReceipt)
                preview.Owner = Application.Current?.MainWindow
                preview.ShowDialog()
                StatusMessage = $"Sale {LastReceipt.ReceiptNumber} completed."
                ClearCart()
            Catch ex As Exception
                StatusMessage = ex.Message
                AppDialogService.ShowError(ex.Message, "Checkout failed")
            End Try
        End Sub

        Private Sub ReprintLastReceipt()
            If LastReceipt Is Nothing Then Return
            Try
                Dim preview As New Views.ReceiptPreviewWindow(LastReceipt)
                preview.Owner = Application.Current?.MainWindow
                preview.ShowDialog()
                StatusMessage = $"Receipt {LastReceipt.ReceiptNumber} ready."
            Catch ex As Exception
                StatusMessage = $"Reprint failed: {ex.Message}"
                AppDialogService.ShowWarning(ex.Message, "Reprint failed")
            End Try
        End Sub
    End Class
End Namespace
