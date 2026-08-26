Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class ServicesViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Private _selectedCategory As String = String.Empty
        Private _selectedSubCategory As String = String.Empty
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _isCategoryManageMode As Boolean
        Private _editingSku As String = String.Empty
        Private _statusMessage As String = String.Empty

        Private _editName As String = String.Empty
        Private _editPrice As Decimal
        Private _editCategory As String = String.Empty
        Private _editSubCategory As String = String.Empty
        Private _editCommissionPercent As Decimal
        Private _pickOneDefaultQty As Decimal = 1D

        Private _selectedManageCategory As CatalogCategoryNode
        Private _selectedManageSubCategory As String
        Private _editCategoryName As String = String.Empty
        Private _editSubCategoryName As String = String.Empty
        Private _isHostedInMasterFiles As Boolean
        Private _stayInCategoryManage As Boolean
        Private _showArchived As Boolean
        Private _showManageCategoriesButton As Boolean = True
        Private _showCategoryCancelButton As Boolean = True
        Private _searchText As String = String.Empty
        Private _isCategoryFormMode As Boolean
        Private _isSubCategoryFormMode As Boolean
        Private _isAddingCategory As Boolean = True
        Private _isAddingSubCategory As Boolean = True

        Public Sub New()
            Services = New ObservableCollection(Of ServiceItem)()
            CategoryChips = New ObservableCollection(Of SelectableChip)()
            SubCategoryChips = New ObservableCollection(Of SelectableChip)()
            EditCategoryOptions = New ObservableCollection(Of String)()
            EditSubCategoryOptions = New ObservableCollection(Of String)()
            FixedConsumableOptions = New ObservableCollection(Of FixedConsumableOption)()
            PickOneConsumableOptions = New ObservableCollection(Of PickOneProductOption)()
            ProductOptions = New ObservableCollection(Of ProductItem)()

            SelectCategoryCommand = New RelayCommand(Of String)(AddressOf SelectCategory)
            SelectSubCategoryCommand = New RelayCommand(Of String)(AddressOf SelectSubCategory)
            AddServiceCommand = New RelayCommand(AddressOf BeginAdd, Function() Not IsCategoryManageMode AndAlso Not String.IsNullOrWhiteSpace(SelectedCategory))
            EditServiceCommand = New RelayCommand(Of ServiceItem)(AddressOf BeginEdit)
            SaveServiceCommand = New RelayCommand(AddressOf SaveService)
            CancelEditCommand = New RelayCommand(AddressOf CancelEdit)
            SelectAllFixedConsumablesCommand = New RelayCommand(AddressOf SelectAllFixedConsumables)
            ClearFixedConsumablesCommand = New RelayCommand(AddressOf ClearFixedConsumables)
            SelectAllPickOneConsumablesCommand = New RelayCommand(AddressOf SelectAllPickOneConsumables)
            ClearPickOneConsumablesCommand = New RelayCommand(AddressOf ClearPickOneConsumables)
            DeleteServiceCommand = New RelayCommand(Of ServiceItem)(AddressOf DeleteService)
            ArchiveServiceCommand = New RelayCommand(Of ServiceItem)(AddressOf ArchiveService)
            UnarchiveServiceCommand = New RelayCommand(Of ServiceItem)(AddressOf UnarchiveService)
            ToggleShowArchivedCommand = New RelayCommand(AddressOf ToggleShowArchived)

            BeginManageCategoriesCommand = New RelayCommand(AddressOf BeginManageCategories, Function() Not IsEditMode AndAlso ShowManageCategoriesButton)
            SelectManageCategoryCommand = New RelayCommand(Of CatalogCategoryNode)(AddressOf SelectManageCategory)
            BeginAddCategoryCommand = New RelayCommand(AddressOf BeginAddCategory, Function() IsCategoryManageMode AndAlso Not IsCategoryFormMode AndAlso Not IsSubCategoryFormMode)
            BeginEditCategoryCommand = New RelayCommand(Of CatalogCategoryNode)(AddressOf BeginEditCategory)
            SaveCategoryFormCommand = New RelayCommand(AddressOf SaveCategoryForm)
            CancelCategoryFormCommand = New RelayCommand(AddressOf CancelCategoryForm)
            DeleteCategoryCommand = New RelayCommand(Of CatalogCategoryNode)(AddressOf DeleteCategory)
            ArchiveCategoryCommand = New RelayCommand(Of CatalogCategoryNode)(AddressOf ArchiveCategory)
            UnarchiveCategoryCommand = New RelayCommand(Of CatalogCategoryNode)(AddressOf UnarchiveCategory)
            BeginAddSubCategoryCommand = New RelayCommand(AddressOf BeginAddSubCategory, Function() IsCategoryManageMode AndAlso SelectedManageCategory IsNot Nothing AndAlso Not IsCategoryFormMode AndAlso Not IsSubCategoryFormMode)
            BeginEditSubCategoryCommand = New RelayCommand(Of String)(AddressOf BeginEditSubCategory)
            SaveSubCategoryFormCommand = New RelayCommand(AddressOf SaveSubCategoryForm)
            CancelSubCategoryFormCommand = New RelayCommand(AddressOf CancelSubCategoryForm)
            DeleteSubCategoryCommand = New RelayCommand(Of String)(AddressOf DeleteSubCategory)
            CancelCategoryManageCommand = New RelayCommand(AddressOf CancelCategoryManage, Function() IsCategoryManageMode AndAlso ShowCategoryCancelButton)

            LoadFromStore()
        End Sub

        Public Sub EnterCategorySection()
            IsEditMode = False
            _stayInCategoryManage = True
            ShowCategoryCancelButton = False
            ShowManageCategoriesButton = False
            BeginManageCategories()
        End Sub

        Public Sub EnterServicesSection()
            _stayInCategoryManage = False
            ShowCategoryCancelButton = True
            ShowManageCategoriesButton = False
            If IsCategoryManageMode Then
                CloseCategoryManagePanel(force:=True)
            End If
            IsEditMode = False
            LoadFromStore()
        End Sub

        Public Sub LeaveMasterSection()
            _stayInCategoryManage = False
            If IsCategoryManageMode Then
                CloseCategoryManagePanel(force:=True)
            End If
            IsEditMode = False
        End Sub

        Public Property IsHostedInMasterFiles As Boolean
            Get
                Return _isHostedInMasterFiles
            End Get
            Set(value As Boolean)
                If SetProperty(_isHostedInMasterFiles, value) AndAlso value Then
                    ShowManageCategoriesButton = False
                End If
            End Set
        End Property

        Public Property ShowManageCategoriesButton As Boolean
            Get
                Return _showManageCategoriesButton
            End Get
            Set(value As Boolean)
                If SetProperty(_showManageCategoriesButton, value) Then
                    BeginManageCategoriesCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property ShowCategoryCancelButton As Boolean
            Get
                Return _showCategoryCancelButton
            End Get
            Set(value As Boolean)
                If SetProperty(_showCategoryCancelButton, value) Then
                    CancelCategoryManageCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property ShowArchived As Boolean
            Get
                Return _showArchived
            End Get
            Set(value As Boolean)
                If SetProperty(_showArchived, value) Then
                    If IsCategoryManageMode Then
                        RefreshManageCategoriesFromStore()
                    Else
                        LoadServices()
                    End If
                    OnPropertyChanged(NameOf(ShowArchivedLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property ShowArchivedLabel As String
            Get
                Return If(ShowArchived, "Hide archived", "Show archived")
            End Get
        End Property

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then
                    If IsCategoryManageMode Then
                        RefreshManageCategoriesFromStore()
                    Else
                        LoadServices()
                    End If
                End If
            End Set
        End Property

        Public Sub LoadFromStore()
            RefreshCategoriesFromStore()
            StatusMessage = String.Empty
        End Sub

        Public Property Services As ObservableCollection(Of ServiceItem)
        Public Property CategoryChips As ObservableCollection(Of SelectableChip)
        Public Property SubCategoryChips As ObservableCollection(Of SelectableChip)
        Public Property EditCategoryOptions As ObservableCollection(Of String)
        Public Property EditSubCategoryOptions As ObservableCollection(Of String)
        Public Property FixedConsumableOptions As ObservableCollection(Of FixedConsumableOption)
        Public Property PickOneConsumableOptions As ObservableCollection(Of PickOneProductOption)
        Public Property ProductOptions As ObservableCollection(Of ProductItem)
        Public Property ManageCategories As ObservableCollection(Of CatalogCategoryNode)
        Public Property ManageSubCategories As ObservableCollection(Of String)

        Public Property SelectedCategory As String
            Get
                Return _selectedCategory
            End Get
            Set(value As String)
                SetProperty(_selectedCategory, value)
            End Set
        End Property

        Public Property SelectedSubCategory As String
            Get
                Return _selectedSubCategory
            End Get
            Set(value As String)
                SetProperty(_selectedSubCategory, value)
            End Set
        End Property

        Public ReadOnly Property HasSubCategories As Boolean
            Get
                Return SubCategoryChips IsNot Nothing AndAlso SubCategoryChips.Count > 0
            End Get
        End Property

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditMode, value) Then
                    BeginManageCategoriesCommand.NotifyCanExecuteChanged()
                    AddServiceCommand.NotifyCanExecuteChanged()
                    OnPropertyChanged(NameOf(ShowListMode))
                End If
            End Set
        End Property

        Public Property IsCategoryManageMode As Boolean
            Get
                Return _isCategoryManageMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isCategoryManageMode, value) Then
                    AddServiceCommand.NotifyCanExecuteChanged()
                    NotifyCategoryManageCommands()
                    OnPropertyChanged(NameOf(ShowListMode))
                    OnPropertyChanged(NameOf(ShowCategoryListMode))
                End If
            End Set
        End Property

        Public Property IsCategoryFormMode As Boolean
            Get
                Return _isCategoryFormMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isCategoryFormMode, value) Then
                    OnPropertyChanged(NameOf(ShowCategoryListMode))
                    OnPropertyChanged(NameOf(CategoryFormTitle))
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public Property IsSubCategoryFormMode As Boolean
            Get
                Return _isSubCategoryFormMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isSubCategoryFormMode, value) Then
                    OnPropertyChanged(NameOf(ShowCategoryListMode))
                    OnPropertyChanged(NameOf(SubCategoryFormTitle))
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public ReadOnly Property ShowListMode As Boolean
            Get
                Return Not IsEditMode AndAlso Not IsCategoryManageMode
            End Get
        End Property

        Public ReadOnly Property ShowCategoryListMode As Boolean
            Get
                Return IsCategoryManageMode AndAlso Not IsCategoryFormMode AndAlso Not IsSubCategoryFormMode
            End Get
        End Property

        Public ReadOnly Property FormTitle As String
            Get
                Return If(_isAdding, "Add service", "Edit service")
            End Get
        End Property

        Public ReadOnly Property CategoryFormTitle As String
            Get
                Return If(_isAddingCategory, "Add category", "Edit category")
            End Get
        End Property

        Public ReadOnly Property SubCategoryFormTitle As String
            Get
                Return If(_isAddingSubCategory, "Add subcategory", "Edit subcategory")
            End Get
        End Property

        Public ReadOnly Property HasSelectedManageCategory As Boolean
            Get
                Return SelectedManageCategory IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property SelectedManageCategoryTitle As String
            Get
                If SelectedManageCategory Is Nothing Then Return "Subcategories"
                Return $"Subcategories · {SelectedManageCategory.Name}"
            End Get
        End Property

        Public Property EditName As String
            Get
                Return _editName
            End Get
            Set(value As String)
                SetProperty(_editName, value)
            End Set
        End Property

        Public Property EditPrice As Decimal
            Get
                Return _editPrice
            End Get
            Set(value As Decimal)
                SetProperty(_editPrice, value)
            End Set
        End Property

        Public Property EditCategory As String
            Get
                Return _editCategory
            End Get
            Set(value As String)
                If SetProperty(_editCategory, value) Then
                    RefreshEditSubCategoryOptions()
                End If
            End Set
        End Property

        Public Property EditSubCategory As String
            Get
                Return _editSubCategory
            End Get
            Set(value As String)
                SetProperty(_editSubCategory, value)
            End Set
        End Property

        Public Property EditCommissionPercent As Decimal
            Get
                Return _editCommissionPercent
            End Get
            Set(value As Decimal)
                SetProperty(_editCommissionPercent, value)
            End Set
        End Property

        Public Property PickOneDefaultQty As Decimal
            Get
                Return _pickOneDefaultQty
            End Get
            Set(value As Decimal)
                SetProperty(_pickOneDefaultQty, value)
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

        Public Property SelectedManageCategory As CatalogCategoryNode
            Get
                Return _selectedManageCategory
            End Get
            Set(value As CatalogCategoryNode)
                If SetProperty(_selectedManageCategory, value) Then
                    RefreshManageSubCategories()
                    OnPropertyChanged(NameOf(HasSelectedManageCategory))
                    OnPropertyChanged(NameOf(SelectedManageCategoryTitle))
                    NotifyCategoryManageCommands()
                End If
            End Set
        End Property

        Public Property SelectedManageSubCategory As String
            Get
                Return _selectedManageSubCategory
            End Get
            Set(value As String)
                SetProperty(_selectedManageSubCategory, value)
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

        Public Property SelectCategoryCommand As RelayCommand(Of String)
        Public Property SelectSubCategoryCommand As RelayCommand(Of String)
        Public Property AddServiceCommand As RelayCommand
        Public Property EditServiceCommand As RelayCommand(Of ServiceItem)
        Public Property SaveServiceCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property SelectAllFixedConsumablesCommand As RelayCommand
        Public Property ClearFixedConsumablesCommand As RelayCommand
        Public Property SelectAllPickOneConsumablesCommand As RelayCommand
        Public Property ClearPickOneConsumablesCommand As RelayCommand
        Public Property DeleteServiceCommand As RelayCommand(Of ServiceItem)
        Public Property ArchiveServiceCommand As RelayCommand(Of ServiceItem)
        Public Property UnarchiveServiceCommand As RelayCommand(Of ServiceItem)
        Public Property ToggleShowArchivedCommand As RelayCommand
        Public Property BeginManageCategoriesCommand As RelayCommand
        Public Property SelectManageCategoryCommand As RelayCommand(Of CatalogCategoryNode)
        Public Property BeginAddCategoryCommand As RelayCommand
        Public Property BeginEditCategoryCommand As RelayCommand(Of CatalogCategoryNode)
        Public Property SaveCategoryFormCommand As RelayCommand
        Public Property CancelCategoryFormCommand As RelayCommand
        Public Property DeleteCategoryCommand As RelayCommand(Of CatalogCategoryNode)
        Public Property ArchiveCategoryCommand As RelayCommand(Of CatalogCategoryNode)
        Public Property UnarchiveCategoryCommand As RelayCommand(Of CatalogCategoryNode)
        Public Property BeginAddSubCategoryCommand As RelayCommand
        Public Property BeginEditSubCategoryCommand As RelayCommand(Of String)
        Public Property SaveSubCategoryFormCommand As RelayCommand
        Public Property CancelSubCategoryFormCommand As RelayCommand
        Public Property DeleteSubCategoryCommand As RelayCommand(Of String)
        Public Property CancelCategoryManageCommand As RelayCommand

        Private Sub ToggleShowArchived()
            ShowArchived = Not ShowArchived
        End Sub

        Private Sub RefreshCategoriesFromStore(Optional selectCategoryName As String = Nothing, Optional selectSubCategoryName As String = Nothing)
            Dim cat = If(Not String.IsNullOrWhiteSpace(selectCategoryName), selectCategoryName, SelectedCategory)
            Dim subCat = If(Not String.IsNullOrWhiteSpace(selectSubCategoryName), selectSubCategoryName, SelectedSubCategory)

            CategoryChips = New ObservableCollection(Of SelectableChip)(
                _store.Categories.Where(Function(c) c.IsActive).Select(Function(c) New SelectableChip With {.Name = c.Name}))
            OnPropertyChanged(NameOf(CategoryChips))
            RefreshEditCategoryOptions()

            If CategoryChips.Count = 0 Then
                SelectedCategory = String.Empty
                SubCategoryChips = New ObservableCollection(Of SelectableChip)()
                OnPropertyChanged(NameOf(SubCategoryChips))
                OnPropertyChanged(NameOf(HasSubCategories))
                Services.Clear()
                Return
            End If

            If Not String.IsNullOrWhiteSpace(cat) AndAlso CategoryChips.Any(Function(c) c.Name.Equals(cat, StringComparison.OrdinalIgnoreCase)) Then
                SelectCategory(cat)
                If Not String.IsNullOrWhiteSpace(subCat) AndAlso SubCategoryChips.Any(Function(s) s.Name.Equals(subCat, StringComparison.OrdinalIgnoreCase)) Then
                    SelectSubCategory(subCat)
                End If
            Else
                SelectCategory(CategoryChips.First().Name)
            End If
        End Sub

        Private Sub SelectCategory(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return
            SelectedCategory = name
            For Each chip In CategoryChips
                chip.IsSelected = chip.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            Next

            Dim node = _store.Categories.FirstOrDefault(Function(c) c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            Dim subs = If(node?.SubCategories, New List(Of String)())
            SubCategoryChips = New ObservableCollection(Of SelectableChip)(
                subs.Select(Function(s) New SelectableChip With {.Name = s}))
            OnPropertyChanged(NameOf(SubCategoryChips))
            OnPropertyChanged(NameOf(HasSubCategories))
            SelectedSubCategory = String.Empty

            If SubCategoryChips.Count > 0 Then
                SelectSubCategory(SubCategoryChips.First().Name)
            Else
                LoadServices()
            End If
            AddServiceCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub SelectSubCategory(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return
            SelectedSubCategory = name
            For Each chip In SubCategoryChips
                chip.IsSelected = chip.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            Next
            LoadServices()
        End Sub

        Private Sub LoadServices()
            Services.Clear()
            Dim query = _store.Services.AsEnumerable()
            If ShowArchived Then
                query = query.Where(Function(x) Not x.IsActive)
            Else
                query = query.Where(Function(x) x.IsActive)
            End If
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                query = query.Where(Function(x) MatchesServiceSearch(x, term))
            Else
                Dim cat = SelectedCategory
                Dim subCat = CurrentSubCategoryValue()
                query = query.Where(Function(x) MatchesLeaf(x.Category, x.SubCategory, cat, subCat))
            End If
            For Each s In query.OrderBy(Function(x) x.Name)
                Services.Add(s)
            Next
        End Sub

        Private Shared Function MatchesServiceSearch(service As ServiceItem, term As String) As Boolean
            Return service.Name.ToLowerInvariant().Contains(term) OrElse
                service.Sku.ToLowerInvariant().Contains(term) OrElse
                (Not String.IsNullOrWhiteSpace(service.Category) AndAlso service.Category.ToLowerInvariant().Contains(term)) OrElse
                (Not String.IsNullOrWhiteSpace(service.SubCategory) AndAlso service.SubCategory.ToLowerInvariant().Contains(term))
        End Function

        Private Function CurrentSubCategoryValue() As String
            Return If(HasSubCategories, SelectedSubCategory, String.Empty)
        End Function

        Private Shared Function MatchesCategorySearch(category As CatalogCategoryNode, term As String) As Boolean
            If category.Name.ToLowerInvariant().Contains(term) Then Return True
            Return category.SubCategories IsNot Nothing AndAlso
                category.SubCategories.Any(Function(s) s.ToLowerInvariant().Contains(term))
        End Function

        Private Shared Function MatchesLeaf(itemCat As String, itemSub As String, cat As String, subCat As String) As Boolean
            If Not String.Equals(itemCat, cat, StringComparison.OrdinalIgnoreCase) Then Return False
            If String.IsNullOrWhiteSpace(subCat) Then
                Return String.IsNullOrWhiteSpace(itemSub)
            End If
            Return String.Equals(itemSub, subCat, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Sub BeginAdd()
            If String.IsNullOrWhiteSpace(SelectedCategory) Then
                StatusMessage = "Select a category first."
                Return
            End If
            _isAdding = True
            _editingSku = String.Empty
            EditName = String.Empty
            EditPrice = 0D
            EditCategory = SelectedCategory
            EditSubCategory = CurrentSubCategoryValue()
            EditCommissionPercent = 0D
            RefreshProductOptions()
            LoadEditConsumables(Nothing)
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(item As ServiceItem)
            If item Is Nothing Then Return
            _isAdding = False
            _editingSku = item.Sku
            EditName = item.Name
            EditPrice = item.Price
            EditCategory = item.Category
            EditSubCategory = item.SubCategory
            EditCommissionPercent = item.CommissionPercent
            RefreshProductOptions()
            LoadEditConsumables(item.Consumables)
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub CancelEdit()
            IsEditMode = False
        End Sub

        Private Sub SaveService()
            If String.IsNullOrWhiteSpace(EditName) Then
                StatusMessage = "Service name is required."
                Return
            End If
            If EditPrice < 0D Then
                StatusMessage = "Price must be zero or greater."
                Return
            End If
            If EditCommissionPercent < 0D Then
                StatusMessage = "Commission must be zero or greater."
                Return
            End If
            If String.IsNullOrWhiteSpace(EditCategory) Then
                StatusMessage = "Category is required."
                Return
            End If

            Dim node = _store.Categories.FirstOrDefault(Function(c) c.IsActive AndAlso c.Name.Equals(EditCategory.Trim(), StringComparison.OrdinalIgnoreCase))
            If node Is Nothing Then
                StatusMessage = "Selected category was not found."
                Return
            End If

            Dim subCat = If(EditSubCategory, String.Empty).Trim()
            If node.SubCategories IsNot Nothing AndAlso node.SubCategories.Count > 0 Then
                If String.IsNullOrWhiteSpace(subCat) Then
                    StatusMessage = "Subcategory is required for this category."
                    Return
                End If
                If Not node.SubCategories.Any(Function(s) s.Equals(subCat, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Selected subcategory was not found."
                    Return
                End If
            Else
                subCat = String.Empty
            End If

            Dim consumables As List(Of ServiceConsumableLine) = New List(Of ServiceConsumableLine)()
            If Not TryBuildConsumablesFromEdit(consumables) Then Return

            If _isAdding Then
                Dim service As New ServiceItem With {
                    .Sku = NextServiceSku(),
                    .Name = EditName.Trim(),
                    .Price = EditPrice,
                    .DurationMinutes = 60,
                    .Icon = "✨",
                    .Category = node.Name,
                    .SubCategory = subCat,
                    .CommissionPercent = EditCommissionPercent,
                    .IsActive = True,
                    .Consumables = consumables
                }
                _store.Services.Add(service)
                StatusMessage = "Service added."
            Else
                Dim existing = _store.Services.FirstOrDefault(Function(s) s.Sku = _editingSku)
                If existing Is Nothing Then
                    StatusMessage = "Service not found."
                    Return
                End If
                existing.Name = EditName.Trim()
                existing.Price = EditPrice
                existing.Category = node.Name
                existing.SubCategory = subCat
                existing.CommissionPercent = EditCommissionPercent
                existing.Consumables = consumables
                StatusMessage = "Service updated."
            End If

            _store.PersistCatalog()
            IsEditMode = False
            RefreshCategoriesFromStore(node.Name, subCat)
        End Sub

        Private Sub DeleteService(item As ServiceItem)
            If item Is Nothing Then Return
            If Not AppDialogService.ConfirmDelete(item.Name) Then Return
            _store.Services.Remove(item)
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} deleted."
            LoadServices()
        End Sub

        Private Sub RefreshProductOptions()
            ProductOptions = New ObservableCollection(Of ProductItem)(
                _store.Products.Where(Function(p) p.IsActive).OrderBy(Function(p) p.Name))
            OnPropertyChanged(NameOf(ProductOptions))
        End Sub

        Private Sub LoadEditConsumables(source As IEnumerable(Of ServiceConsumableLine))
            Dim lines = If(source, Enumerable.Empty(Of ServiceConsumableLine)()).ToList()

            Dim fixedBySku = lines.
                Where(Function(c) c.Kind = ServiceConsumableKind.Fixed AndAlso Not String.IsNullOrWhiteSpace(c.ProductSku)).
                GroupBy(Function(c) c.ProductSku, StringComparer.OrdinalIgnoreCase).
                ToDictionary(Function(g) g.Key, Function(g) g.First(), StringComparer.OrdinalIgnoreCase)

            Dim pickOneSkus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim pickOneQty As Decimal = 1D
            For Each pickLine In lines.Where(Function(c) c.Kind = ServiceConsumableKind.PickOne)
                If pickLine.OptionProductSkus IsNot Nothing Then
                    For Each sku In pickLine.OptionProductSkus
                        If Not String.IsNullOrWhiteSpace(sku) Then pickOneSkus.Add(sku)
                    Next
                End If
                If pickLine.Quantity > 0D Then pickOneQty = pickLine.Quantity
            Next

            FixedConsumableOptions = New ObservableCollection(Of FixedConsumableOption)(
                ProductOptions.Select(Function(p)
                                          Dim fixedLine As ServiceConsumableLine = Nothing
                                          fixedBySku.TryGetValue(p.Sku, fixedLine)
                                          Return New FixedConsumableOption With {
                                              .Sku = p.Sku,
                                              .Name = p.Name,
                                              .IsSelected = fixedLine IsNot Nothing,
                                              .Quantity = If(fixedLine IsNot Nothing AndAlso fixedLine.Quantity > 0D, fixedLine.Quantity, 1D)
                                          }
                                      End Function))

            PickOneConsumableOptions = New ObservableCollection(Of PickOneProductOption)(
                ProductOptions.Select(Function(p) New PickOneProductOption With {
                    .Sku = p.Sku,
                    .Name = p.Name,
                    .IsSelected = pickOneSkus.Contains(p.Sku)
                }))

            PickOneDefaultQty = pickOneQty

            OnPropertyChanged(NameOf(FixedConsumableOptions))
            OnPropertyChanged(NameOf(PickOneConsumableOptions))
        End Sub

        Private Sub SelectAllFixedConsumables()
            For Each opt In FixedConsumableOptions
                opt.IsSelected = True
            Next
        End Sub

        Private Sub ClearFixedConsumables()
            For Each opt In FixedConsumableOptions
                opt.IsSelected = False
            Next
        End Sub

        Private Sub SelectAllPickOneConsumables()
            For Each opt In PickOneConsumableOptions
                opt.IsSelected = True
            Next
        End Sub

        Private Sub ClearPickOneConsumables()
            For Each opt In PickOneConsumableOptions
                opt.IsSelected = False
            Next
        End Sub

        Private Function TryBuildConsumablesFromEdit(ByRef consumables As List(Of ServiceConsumableLine)) As Boolean
            consumables = New List(Of ServiceConsumableLine)

            For Each opt In FixedConsumableOptions.Where(Function(o) o.IsSelected)
                If opt.Quantity <= 0D Then
                    StatusMessage = "Each always-deduct product must use a quantity greater than zero."
                    Return False
                End If

                Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku.Equals(opt.Sku, StringComparison.OrdinalIgnoreCase))
                If product Is Nothing OrElse Not product.IsActive Then
                    StatusMessage = "One or more always-deduct products were not found or are archived."
                    Return False
                End If

                consumables.Add(New ServiceConsumableLine With {
                    .Kind = ServiceConsumableKind.Fixed,
                    .ProductSku = product.Sku,
                    .Quantity = opt.Quantity
                })
            Next

            Dim pickSelected = PickOneConsumableOptions.Where(Function(o) o.IsSelected).Select(Function(o) o.Sku).ToList()
            If pickSelected.Count > 0 Then
                If PickOneDefaultQty <= 0D Then
                    StatusMessage = "Default quantity for pick-at-POS must be greater than zero."
                    Return False
                End If
                If pickSelected.Count < 2 Then
                    StatusMessage = "Select at least two products for pick-at-POS, or use Always deduct for a single product."
                    Return False
                End If

                For Each sku In pickSelected
                    Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))
                    If product Is Nothing OrElse Not product.IsActive Then
                        StatusMessage = "One or more pick-at-POS products were not found or are archived."
                        Return False
                    End If
                Next

                consumables.Add(New ServiceConsumableLine With {
                    .Kind = ServiceConsumableKind.PickOne,
                    .Quantity = PickOneDefaultQty,
                    .OptionProductSkus = pickSelected
                })
            End If

            Return True
        End Function

        Private Sub ArchiveService(item As ServiceItem)
            If item Is Nothing OrElse Not item.IsActive Then Return
            item.IsActive = False
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} archived."
            LoadServices()
        End Sub

        Private Sub UnarchiveService(item As ServiceItem)
            If item Is Nothing OrElse item.IsActive Then Return
            item.IsActive = True
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} restored."
            LoadServices()
        End Sub

        Private Function NextServiceSku() As String
            Dim maxNum = 0
            For Each s In _store.Services
                If s.Sku Is Nothing OrElse s.Sku.Length < 3 Then Continue For
                If Not s.Sku.StartsWith("CS", StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim n As Integer
                If Integer.TryParse(s.Sku.Substring(2), n) Then
                    maxNum = Math.Max(maxNum, n)
                End If
            Next
            Return $"CS{(maxNum + 1):D3}"
        End Function

        Private Sub RefreshEditCategoryOptions()
            EditCategoryOptions = New ObservableCollection(Of String)(
                _store.Categories.Where(Function(c) c.IsActive).Select(Function(c) c.Name))
            OnPropertyChanged(NameOf(EditCategoryOptions))
            RefreshEditSubCategoryOptions()
        End Sub

        Private Sub RefreshEditSubCategoryOptions()
            Dim node = _store.Categories.FirstOrDefault(Function(c) c.Name.Equals(EditCategory, StringComparison.OrdinalIgnoreCase))
            Dim subs = If(node?.SubCategories, New List(Of String)())
            EditSubCategoryOptions = New ObservableCollection(Of String)(subs)
            OnPropertyChanged(NameOf(EditSubCategoryOptions))
            If Not String.IsNullOrWhiteSpace(EditSubCategory) AndAlso
               Not EditSubCategoryOptions.Any(Function(s) s.Equals(EditSubCategory, StringComparison.OrdinalIgnoreCase)) Then
                EditSubCategory = If(EditSubCategoryOptions.FirstOrDefault(), String.Empty)
            ElseIf String.IsNullOrWhiteSpace(EditSubCategory) AndAlso EditSubCategoryOptions.Count > 0 Then
                EditSubCategory = EditSubCategoryOptions.First()
            End If
        End Sub

        Private Sub BeginManageCategories()
            IsCategoryFormMode = False
            IsSubCategoryFormMode = False
            StatusMessage = String.Empty
            IsCategoryManageMode = True
            RefreshManageCategoriesFromStore()
        End Sub

        Private Sub RefreshManageCategoriesFromStore(Optional selectName As String = Nothing)
            Dim keepName = If(Not String.IsNullOrWhiteSpace(selectName), selectName, SelectedManageCategory?.Name)
            Dim query = _store.Categories.AsEnumerable()
            If ShowArchived Then
                query = query.Where(Function(c) Not c.IsActive)
            Else
                query = query.Where(Function(c) c.IsActive)
            End If
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                query = query.Where(Function(c) MatchesCategorySearch(c, term))
            End If
            ManageCategories = New ObservableCollection(Of CatalogCategoryNode)(query.OrderBy(Function(c) c.Name))
            OnPropertyChanged(NameOf(ManageCategories))

            If Not String.IsNullOrWhiteSpace(keepName) Then
                SelectedManageCategory = ManageCategories.FirstOrDefault(Function(c) c.Name.Equals(keepName, StringComparison.OrdinalIgnoreCase))
            Else
                SelectedManageCategory = ManageCategories.FirstOrDefault()
            End If
        End Sub

        Private Sub SelectManageCategory(node As CatalogCategoryNode)
            If node Is Nothing Then Return
            SelectedManageCategory = node
        End Sub

        Private Sub CancelCategoryManage()
            If Not ShowCategoryCancelButton Then Return
            CloseCategoryManagePanel()
        End Sub

        Private Sub CloseCategoryManagePanel(Optional force As Boolean = False)
            If Not force AndAlso _stayInCategoryManage Then Return
            IsCategoryFormMode = False
            IsSubCategoryFormMode = False
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

        Private Sub BeginAddCategory()
            _isAddingCategory = True
            SelectedManageCategory = Nothing
            EditCategoryName = String.Empty
            IsCategoryFormMode = True
        End Sub

        Private Sub BeginEditCategory(node As CatalogCategoryNode)
            If node Is Nothing Then Return
            _isAddingCategory = False
            SelectedManageCategory = node
            EditCategoryName = node.Name
            IsCategoryFormMode = True
        End Sub

        Private Sub CancelCategoryForm()
            IsCategoryFormMode = False
            EditCategoryName = String.Empty
        End Sub

        Private Sub SaveCategoryForm()
            Dim name = EditCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(name) Then
                StatusMessage = "Category name is required."
                Return
            End If

            If _isAddingCategory Then
                If _store.Categories.Any(Function(c) c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Category name already exists."
                    Return
                End If
                Dim node = New CatalogCategoryNode With {.Name = name, .IsActive = True}
                _store.Categories.Add(node)
                _store.PersistCatalog()
                IsCategoryFormMode = False
                RefreshManageCategoriesFromStore(name)
                StatusMessage = "Category added."
            Else
                If SelectedManageCategory Is Nothing Then Return
                If _store.Categories.Any(Function(c) Not Object.ReferenceEquals(c, SelectedManageCategory) AndAlso c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Category name already exists."
                    Return
                End If
                Dim oldName = SelectedManageCategory.Name
                If Not oldName.Equals(name, StringComparison.OrdinalIgnoreCase) Then
                    SelectedManageCategory.Name = name
                    UpdateCategoryReferences(oldName, name, Nothing, Nothing)
                End If
                _store.PersistCatalog()
                IsCategoryFormMode = False
                RefreshManageCategoriesFromStore(name)
                StatusMessage = "Category updated."
            End If
        End Sub

        Private Sub DeleteCategory(node As CatalogCategoryNode)
            If node Is Nothing Then Return
            Dim categoryName = node.Name
            Dim serviceCount = _store.Services.Where(Function(s) s.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim productCount = _store.Products.Where(Function(p) p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim itemCount = serviceCount + productCount

            If itemCount > 0 Then
                If Not AppDialogService.ConfirmDelete(categoryName, $"{itemCount} catalog item(s) still use category '{categoryName}'.") Then Return
            Else
                If Not AppDialogService.ConfirmDelete(categoryName) Then Return
            End If

            _store.Categories.Remove(node)
            _store.PersistCatalog()
            StatusMessage = $"{categoryName} deleted."
            RefreshManageCategoriesFromStore()
        End Sub

        Private Sub ArchiveCategory(node As CatalogCategoryNode)
            If node Is Nothing OrElse Not node.IsActive Then Return
            node.IsActive = False
            _store.PersistCatalog()
            StatusMessage = $"{node.Name} archived."
            RefreshManageCategoriesFromStore(If(ShowArchived, node.Name, Nothing))
        End Sub

        Private Sub UnarchiveCategory(node As CatalogCategoryNode)
            If node Is Nothing OrElse node.IsActive Then Return
            node.IsActive = True
            _store.PersistCatalog()
            StatusMessage = $"{node.Name} restored."
            RefreshManageCategoriesFromStore(node.Name)
        End Sub

        Private Sub BeginAddSubCategory()
            If SelectedManageCategory Is Nothing Then Return
            _isAddingSubCategory = True
            SelectedManageSubCategory = Nothing
            EditSubCategoryName = String.Empty
            IsSubCategoryFormMode = True
        End Sub

        Private Sub BeginEditSubCategory(name As String)
            If SelectedManageCategory Is Nothing OrElse String.IsNullOrWhiteSpace(name) Then Return
            _isAddingSubCategory = False
            SelectedManageSubCategory = name
            EditSubCategoryName = name
            IsSubCategoryFormMode = True
        End Sub

        Private Sub CancelSubCategoryForm()
            IsSubCategoryFormMode = False
            EditSubCategoryName = String.Empty
            SelectedManageSubCategory = Nothing
        End Sub

        Private Sub SaveSubCategoryForm()
            If SelectedManageCategory Is Nothing Then Return
            Dim name = EditSubCategoryName?.Trim()
            If String.IsNullOrWhiteSpace(name) Then
                StatusMessage = "Subcategory name is required."
                Return
            End If

            If _isAddingSubCategory Then
                If SelectedManageCategory.SubCategories.Any(Function(s) s.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Subcategory name already exists."
                    Return
                End If
                SelectedManageCategory.SubCategories.Add(name)
                _store.PersistCatalog()
                IsSubCategoryFormMode = False
                RefreshManageSubCategories()
                StatusMessage = "Subcategory added."
            Else
                If String.IsNullOrWhiteSpace(SelectedManageSubCategory) Then Return
                If SelectedManageCategory.SubCategories.Any(Function(s) Not s.Equals(SelectedManageSubCategory, StringComparison.OrdinalIgnoreCase) AndAlso s.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Subcategory name already exists."
                    Return
                End If
                Dim index = SelectedManageCategory.SubCategories.FindIndex(Function(s) s.Equals(SelectedManageSubCategory, StringComparison.OrdinalIgnoreCase))
                If index < 0 Then Return
                If Not SelectedManageSubCategory.Equals(name, StringComparison.OrdinalIgnoreCase) Then
                    Dim oldName = SelectedManageSubCategory
                    SelectedManageCategory.SubCategories(index) = name
                    UpdateCategoryReferences(SelectedManageCategory.Name, SelectedManageCategory.Name, oldName, name)
                End If
                _store.PersistCatalog()
                IsSubCategoryFormMode = False
                RefreshManageSubCategories()
                StatusMessage = "Subcategory updated."
            End If
        End Sub

        Private Sub DeleteSubCategory(subName As String)
            If SelectedManageCategory Is Nothing OrElse String.IsNullOrWhiteSpace(subName) Then Return
            Dim categoryName = SelectedManageCategory.Name
            Dim serviceCount = _store.Services.Where(Function(s) s.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase) AndAlso s.SubCategory.Equals(subName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim productCount = _store.Products.Where(Function(p) p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase) AndAlso p.SubCategory.Equals(subName, StringComparison.OrdinalIgnoreCase)).Count()
            Dim itemCount = serviceCount + productCount

            If itemCount > 0 Then
                If Not AppDialogService.ConfirmDelete(subName, $"{itemCount} catalog item(s) still use subcategory '{subName}'.") Then Return
            Else
                If Not AppDialogService.ConfirmDelete(subName) Then Return
            End If

            SelectedManageCategory.SubCategories.RemoveAll(Function(s) s.Equals(subName, StringComparison.OrdinalIgnoreCase))
            _store.PersistCatalog()
            RefreshManageSubCategories()
            StatusMessage = $"{subName} deleted."
        End Sub

        Private Sub RefreshManageSubCategories()
            Dim subs = If(SelectedManageCategory?.SubCategories, New List(Of String)())
            ManageSubCategories = New ObservableCollection(Of String)(subs.OrderBy(Function(s) s))
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

        Private Sub NotifyCategoryManageCommands()
            BeginAddCategoryCommand.NotifyCanExecuteChanged()
            BeginAddSubCategoryCommand.NotifyCanExecuteChanged()
            CancelCategoryManageCommand.NotifyCanExecuteChanged()
        End Sub
    End Class
End Namespace
