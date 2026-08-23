Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports HairSalonPOS.Wpf.Helpers

Namespace ViewModels
    Public NotInheritable Class InventoryTabs
        Public Const Products As String = "Products"
        Public Const StockIn As String = "StockIn"
        Public Const StockOut As String = "StockOut"
        Public Const MovementLog As String = "MovementLog"
    End Class

    Public Class InventoryViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _inventory As New InventoryService()
        Private ReadOnly _images As CatalogImageService = CatalogImageService.Instance

        Private _searchText As String = String.Empty
        Private _selectedProduct As ProductItem
        Private _activeTab As String = InventoryTabs.Products
        Private _showLowStockOnly As Boolean
        Private _editSku As String = String.Empty
        Private _editName As String = String.Empty
        Private _editBrand As String = String.Empty
        Private _editPrice As Decimal
        Private _editQty As Integer
        Private _editReorder As Integer = 10
        Private _editCategory As String = String.Empty
        Private _editSubCategory As String = String.Empty
        Private _editImagePath As String = String.Empty
        Private _pendingSourcePath As String
        Private _originalImagePath As String = String.Empty
        Private _imageRemoved As Boolean
        Private _isEditMode As Boolean
        Private _isAdding As Boolean
        Private _statusMessage As String = String.Empty
        Private _suppressStockPrompt As Boolean
        Private _stockDialogOpen As Boolean
        Private _lastStockCommandUtc As DateTime = DateTime.MinValue

        Public Sub New()
            Products = New ObservableCollection(Of ProductItem)()
            Movements = New ObservableCollection(Of StockMovement)()
            ProductMovements = New ObservableCollection(Of StockMovement)()
            EditCategoryOptions = New ObservableCollection(Of String)()
            EditSubCategoryOptions = New ObservableCollection(Of String)()

            RefreshCommand = New RelayCommand(AddressOf LoadAll)
            ShowProductsTabCommand = New RelayCommand(Sub() ActiveTab = InventoryTabs.Products)
            ShowStockInTabCommand = New RelayCommand(Sub() ActiveTab = InventoryTabs.StockIn)
            ShowStockOutTabCommand = New RelayCommand(Sub() ActiveTab = InventoryTabs.StockOut)
            ShowMovementLogCommand = New RelayCommand(Sub() ActiveTab = InventoryTabs.MovementLog)
            AddProductCommand = New RelayCommand(AddressOf BeginAddProduct)
            EditProductCommand = New RelayCommand(AddressOf BeginEditProduct, Function() SelectedProduct IsNot Nothing AndAlso IsProductsTab)
            SaveProductCommand = New RelayCommand(AddressOf SaveProduct)
            CancelEditCommand = New RelayCommand(AddressOf CancelEdit)
            DeleteProductCommand = New RelayCommand(AddressOf DeleteSelected, Function() SelectedProduct IsNot Nothing AndAlso IsProductsTab)
            ExportCommand = New RelayCommand(AddressOf ExportInventory)
            ChooseImageCommand = New RelayCommand(AddressOf ChooseImage)
            RemoveImageCommand = New RelayCommand(AddressOf RemoveImage)
            ClearLowStockFilterCommand = New RelayCommand(AddressOf ClearLowStockFilter, Function() ShowLowStockOnly)

            AddHandler _store.SaleCompleted, Sub() LoadAll()
            AddHandler _store.InventoryChanged, Sub() LoadMovements()
            LoadAll()
        End Sub

        Public Property Products As ObservableCollection(Of ProductItem)
        Public Property Movements As ObservableCollection(Of StockMovement)
        Public Property ProductMovements As ObservableCollection(Of StockMovement)
        Public Property EditCategoryOptions As ObservableCollection(Of String)
        Public Property EditSubCategoryOptions As ObservableCollection(Of String)

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                SetProperty(_searchText, value)
                LoadProducts()
            End Set
        End Property

        Public Property SelectedProduct As ProductItem
            Get
                Return _selectedProduct
            End Get
            Set(value As ProductItem)
                If SetProperty(_selectedProduct, value) Then
                    EditProductCommand.NotifyCanExecuteChanged()
                    DeleteProductCommand.NotifyCanExecuteChanged()
                    OnPropertyChanged(NameOf(HasSelectedProduct))
                    OnPropertyChanged(NameOf(HasProductMovements))
                    OnPropertyChanged(NameOf(ShowProductQuickActions))
                    RefreshProductMovements()
                End If
            End Set
        End Property

        Public Property ActiveTab As String
            Get
                Return _activeTab
            End Get
            Set(value As String)
                If SetProperty(_activeTab, value) Then
                    NotifyTabPropertiesChanged()
                End If
            End Set
        End Property

        Public ReadOnly Property HasSelectedProduct As Boolean
            Get
                Return SelectedProduct IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property HasProducts As Boolean
            Get
                Return Products IsNot Nothing AndAlso Products.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasProductMovements As Boolean
            Get
                Return ProductMovements IsNot Nothing AndAlso ProductMovements.Count > 0
            End Get
        End Property

        Public ReadOnly Property ShowProductList As Boolean
            Get
                Return IsProductsTab OrElse IsStockInTab OrElse IsStockOutTab
            End Get
        End Property

        Public ReadOnly Property ShowMovementLog As Boolean
            Get
                Return ActiveTab = InventoryTabs.MovementLog
            End Get
        End Property

        Public ReadOnly Property IsProductsTab As Boolean
            Get
                Return ActiveTab = InventoryTabs.Products
            End Get
        End Property

        Public ReadOnly Property IsStockInTab As Boolean
            Get
                Return ActiveTab = InventoryTabs.StockIn
            End Get
        End Property

        Public ReadOnly Property IsStockOutTab As Boolean
            Get
                Return ActiveTab = InventoryTabs.StockOut
            End Get
        End Property

        Public ReadOnly Property TabHintText As String
            Get
                Select Case ActiveTab
                    Case InventoryTabs.StockIn
                        Return "Double-click a product to add stock."
                    Case InventoryTabs.StockOut
                        Return "Double-click a product to deduct stock (damage, expired, used, etc.)."
                    Case Else
                        Return String.Empty
                End Select
            End Get
        End Property

        Public ReadOnly Property ShowTabHint As Boolean
            Get
                Return IsStockInTab OrElse IsStockOutTab
            End Get
        End Property

        Public ReadOnly Property ShowProductQuickActions As Boolean
            Get
                Return IsProductsTab AndAlso HasSelectedProduct AndAlso Not IsEditMode
            End Get
        End Property

        Public Property ShowLowStockOnly As Boolean
            Get
                Return _showLowStockOnly
            End Get
            Set(value As Boolean)
                If SetProperty(_showLowStockOnly, value) Then
                    OnPropertyChanged(NameOf(LowStockFilterLabel))
                    LoadProducts()
                End If
            End Set
        End Property

        Public ReadOnly Property LowStockFilterLabel As String
            Get
                Return If(ShowLowStockOnly, "Low / out stock only", String.Empty)
            End Get
        End Property

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditMode, value) Then
                    OnPropertyChanged(NameOf(ShowProductQuickActions))
                    EditProductCommand.NotifyCanExecuteChanged()
                    DeleteProductCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public ReadOnly Property ShowEditQty As Boolean
            Get
                Return _isAdding
            End Get
        End Property

        Public ReadOnly Property FormTitle As String
            Get
                Return If(_isAdding, "Add product", "Edit product")
            End Get
        End Property

        Public Property EditSku As String
            Get
                Return _editSku
            End Get
            Set(value As String)
                SetProperty(_editSku, value)
            End Set
        End Property

        Public Property EditName As String
            Get
                Return _editName
            End Get
            Set(value As String)
                SetProperty(_editName, value)
                OnPropertyChanged(NameOf(EditPlaceholderIcon))
            End Set
        End Property

        Public Property EditBrand As String
            Get
                Return _editBrand
            End Get
            Set(value As String)
                SetProperty(_editBrand, value)
                OnPropertyChanged(NameOf(EditPlaceholderIcon))
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

        Public Property EditQty As Integer
            Get
                Return _editQty
            End Get
            Set(value As Integer)
                SetProperty(_editQty, value)
            End Set
        End Property

        Public Property EditReorder As Integer
            Get
                Return _editReorder
            End Get
            Set(value As Integer)
                SetProperty(_editReorder, value)
            End Set
        End Property

        Public Property EditCategory As String
            Get
                Return _editCategory
            End Get
            Set(value As String)
                If SetProperty(_editCategory, value) Then
                    RefreshEditSubCategoryOptions()
                    OnPropertyChanged(NameOf(EditPlaceholderIcon))
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

        Public Property EditImagePath As String
            Get
                Return _editImagePath
            End Get
            Set(value As String)
                If SetProperty(_editImagePath, If(value, String.Empty)) Then
                    OnPropertyChanged(NameOf(HasEditImage))
                    OnPropertyChanged(NameOf(ChooseImageLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property HasEditImage As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(EditImagePath)
            End Get
        End Property

        Public ReadOnly Property ChooseImageLabel As String
            Get
                Return If(HasEditImage, "Change photo", "Choose photo")
            End Get
        End Property

        Public ReadOnly Property EditPlaceholderIcon As String
            Get
                Return ProductPlaceholderIcons.ResolveFromText($"{EditName} {EditBrand} {EditCategory} {EditSubCategory}")
            End Get
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property RefreshCommand As RelayCommand
        Public Property ShowProductsTabCommand As RelayCommand
        Public Property ShowStockInTabCommand As RelayCommand
        Public Property ShowStockOutTabCommand As RelayCommand
        Public Property ShowMovementLogCommand As RelayCommand
        Public Property AddProductCommand As RelayCommand
        Public Property EditProductCommand As RelayCommand
        Public Property SaveProductCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteProductCommand As RelayCommand
        Public Property ExportCommand As RelayCommand
        Public Property ChooseImageCommand As RelayCommand
        Public Property RemoveImageCommand As RelayCommand
        Public Property ClearLowStockFilterCommand As RelayCommand

        Public Sub ApplyLowStockFilter()
            ActiveTab = InventoryTabs.Products
            ShowLowStockOnly = True
            StatusMessage = "Showing products that are low or out of stock."
        End Sub

        Public Sub OpenStockMovementForProduct(product As ProductItem)
            If IsEditMode OrElse _stockDialogOpen Then Return
            If Not IsStockInTab AndAlso Not IsStockOutTab Then Return
            If product Is Nothing Then Return

            Dim resolved = ResolveListedProduct(product)
            If resolved Is Nothing Then Return

            If Not Object.ReferenceEquals(SelectedProduct, resolved) Then
                _suppressStockPrompt = True
                Try
                    SelectedProduct = resolved
                Finally
                    _suppressStockPrompt = False
                End Try
            End If

            Try
                Select Case ActiveTab
                    Case InventoryTabs.StockIn
                        CompleteStockIn(resolved)
                    Case InventoryTabs.StockOut
                        CompleteStockOut(resolved)
                End Select
            Catch ex As Exception
                Dim title = If(IsStockOutTab, "Stock out", "Stock in")
                ErrorLogService.LogException($"OpenStockMovementForProduct — {resolved.Sku} {resolved.Name}", ex)
                AppDialogService.ShowError(
                    $"Could not open stock movement for {resolved.Name}.{Environment.NewLine}{Environment.NewLine}{ErrorLogService.Describe(ex)}",
                    title)
            End Try
        End Sub

        Private Function ResolveListedProduct(product As ProductItem) As ProductItem
            If product Is Nothing OrElse Products Is Nothing Then Return Nothing
            Return Products.FirstOrDefault(Function(p) p.Sku = product.Sku)
        End Function

        Private Shared Function CurrentUserNameOrThrow() As String
            Dim user = SessionContext.CurrentUser
            If user Is Nothing OrElse String.IsNullOrWhiteSpace(user.FullName) Then
                Throw New InvalidOperationException("You must be logged in to record stock movements.")
            End If
            Return user.FullName
        End Function

        Private Sub NotifyTabPropertiesChanged()
            OnPropertyChanged(NameOf(ShowProductList))
            OnPropertyChanged(NameOf(ShowMovementLog))
            OnPropertyChanged(NameOf(IsProductsTab))
            OnPropertyChanged(NameOf(IsStockInTab))
            OnPropertyChanged(NameOf(IsStockOutTab))
            OnPropertyChanged(NameOf(TabHintText))
            OnPropertyChanged(NameOf(ShowTabHint))
            OnPropertyChanged(NameOf(ShowProductQuickActions))
            EditProductCommand.NotifyCanExecuteChanged()
            DeleteProductCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub ClearLowStockFilter()
            ShowLowStockOnly = False
            StatusMessage = String.Empty
        End Sub

        Public Sub LoadProducts()
            Dim sku = SelectedProduct?.Sku
            Dim query = _store.Products.Where(Function(p) p.IsActive)
            If ShowLowStockOnly Then
                query = query.Where(Function(p) p.StockOnHand <= p.ReorderLevel)
            End If
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                query = query.Where(Function(p) p.Name.ToLower().Contains(term) OrElse
                    p.Sku.ToLower().Contains(term) OrElse
                    (p.Brand IsNot Nothing AndAlso p.Brand.ToLower().Contains(term)))
            End If
            Products = New ObservableCollection(Of ProductItem)(query.OrderBy(Function(p) p.Name).ToList())
            OnPropertyChanged(NameOf(Products))
            OnPropertyChanged(NameOf(HasProducts))

            Dim match As ProductItem = Nothing
            If Not String.IsNullOrWhiteSpace(sku) Then
                match = Products.FirstOrDefault(Function(p) p.Sku = sku)
            End If
            If match Is Nothing AndAlso Products.Count > 0 Then
                match = Products(0)
            End If
            _suppressStockPrompt = True
            Try
                SelectedProduct = match
            Finally
                _suppressStockPrompt = False
            End Try
        End Sub

        Public Sub LoadMovements()
            Movements = New ObservableCollection(Of StockMovement)(_store.StockMovements.OrderByDescending(Function(m) m.CreatedAt))
            OnPropertyChanged(NameOf(Movements))
            RefreshProductMovements()
        End Sub

        Public Sub LoadAll()
            LoadProducts()
            LoadMovements()
        End Sub

        Private Sub RefreshProductMovements()
            Dim sku = SelectedProduct?.Sku
            Dim rows As IEnumerable(Of StockMovement) = Enumerable.Empty(Of StockMovement)()
            If Not String.IsNullOrWhiteSpace(sku) Then
                rows = Movements.Where(Function(m) m.Sku = sku).Take(8)
            End If
            ProductMovements = New ObservableCollection(Of StockMovement)(rows)
            OnPropertyChanged(NameOf(ProductMovements))
            OnPropertyChanged(NameOf(HasProductMovements))
        End Sub

        Private Sub BeginAddProduct()
            RefreshEditCategoryOptions()
            If EditCategoryOptions.Count = 0 Then
                StatusMessage = "Add an active category first."
                Return
            End If

            _isAdding = True
            _suppressStockPrompt = True
            Try
                SelectedProduct = Nothing
            Finally
                _suppressStockPrompt = False
            End Try
            IsEditMode = True
            EditSku = ProductSkuService.NextProductSku(_store.Products)
            EditName = String.Empty
            EditBrand = String.Empty
            EditPrice = 0D
            EditQty = 0
            EditReorder = 10
            EditCategory = EditCategoryOptions.First()
            EditSubCategory = String.Empty
            ResetImageEdit(String.Empty)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(ShowEditQty))
        End Sub

        Private Sub BeginEditProduct()
            If SelectedProduct Is Nothing Then Return
            RefreshEditCategoryOptions()
            _isAdding = False
            IsEditMode = True
            EditSku = SelectedProduct.Sku
            EditName = SelectedProduct.Name
            EditBrand = SelectedProduct.Brand
            EditPrice = SelectedProduct.Price
            EditQty = SelectedProduct.StockOnHand
            EditReorder = SelectedProduct.ReorderLevel
            EditCategory = SelectedProduct.Category
            EditSubCategory = SelectedProduct.SubCategory
            ResetImageEdit(SelectedProduct.ImagePath)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(ShowEditQty))
        End Sub

        Private Sub SaveProduct()
            If String.IsNullOrWhiteSpace(EditName) Then
                StatusMessage = "Product name is required."
                Return
            End If
            If EditPrice < 0D Then
                StatusMessage = "Price must be zero or greater."
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

            Try
                Dim imagePath = CommitImage(EditSku.Trim())
                If imagePath Is Nothing AndAlso _pendingSourcePath IsNot Nothing Then
                    StatusMessage = "Could not save the photo."
                    Return
                End If
                Dim product As New ProductItem With {
                    .Sku = EditSku.Trim(),
                    .Name = EditName.Trim(),
                    .Brand = EditBrand.Trim(),
                    .Price = EditPrice,
                    .StockOnHand = If(_isAdding, EditQty, If(SelectedProduct?.StockOnHand, 0)),
                    .ReorderLevel = EditReorder,
                    .Category = node.Name,
                    .SubCategory = subCat,
                    .ImagePath = If(imagePath, String.Empty)
                }
                _inventory.SaveProduct(product, _isAdding, CurrentUserNameOrThrow())
                IsEditMode = False
                _isAdding = False
                StatusMessage = "Product saved."
                LoadAll()
            Catch ex As Exception
                StatusMessage = ex.Message
            End Try
        End Sub

        Private Sub CancelEdit()
            IsEditMode = False
            _isAdding = False
            _suppressStockPrompt = True
            Try
                If SelectedProduct Is Nothing AndAlso Products.Count > 0 Then
                    SelectedProduct = Products(0)
                End If
            Finally
                _suppressStockPrompt = False
            End Try
        End Sub

        Private Sub DeleteSelected()
            Dim product = SelectedProduct
            If product Is Nothing Then Return
            If Not SessionContext.IsAdmin Then
                StatusMessage = "Only Admin can manage inventory."
                Return
            End If
            If Not AppDialogService.ConfirmDelete(product.Name) Then Return

            Try
                _inventory.DeleteProduct(product)
                StatusMessage = $"{product.Name} deleted."
                LoadAll()
            Catch ex As Exception
                StatusMessage = ex.Message
            End Try
        End Sub

        Private Sub ExportInventory()
            Dim salonName = AppSettingsService.Instance.Settings.SalonName
            If ExportService.ExportInventoryPdf(_store.Products.Where(Function(p) p.IsActive), $"{salonName} Inventory") Then
                StatusMessage = "Inventory exported as PDF."
            End If
        End Sub

        Private Function CompleteStockIn(product As ProductItem, Optional suggestedQty As Integer = 1) As Boolean
            If product Is Nothing OrElse Not TryBeginStockCommand(product) Then Return False
            _stockDialogOpen = True
            Try
                product.EnsureDefaults()
                Dim prompt = AppDialogService.PromptStockMovement(product, True, initialQty:=suggestedQty)
                If prompt Is Nothing Then Return False
                _inventory.StockIn(product.Sku, prompt.Quantity, CurrentUserNameOrThrow(), prompt.CombinedNotes)
                StatusMessage = $"Stocked in {prompt.Quantity} of {product.Name}."
                LoadAll()
                Return True
            Catch ex As InvalidOperationException
                AppDialogService.ShowError(ex.Message, "Stock in")
                Return False
            Catch ex As Exception
                ReportStockFailure("Stock in", "CompleteStockIn", product, ex)
                Return False
            Finally
                _stockDialogOpen = False
            End Try
        End Function

        Private Function CompleteStockOut(product As ProductItem) As Boolean
            If product Is Nothing OrElse Not TryBeginStockCommand(product) Then Return False
            _stockDialogOpen = True
            Try
                product.EnsureDefaults()
                Dim prompt = AppDialogService.PromptStockMovement(product, False)
                If prompt Is Nothing Then Return False
                _inventory.StockOut(product.Sku, prompt.Quantity, CurrentUserNameOrThrow(), prompt.CombinedNotes)
                StatusMessage = $"Stocked out {prompt.Quantity} of {product.Name}."
                LoadAll()
                Return True
            Catch ex As InvalidOperationException
                AppDialogService.ShowError(ex.Message, "Stock out")
                Return False
            Catch ex As Exception
                ReportStockFailure("Stock out", "CompleteStockOut", product, ex)
                Return False
            Finally
                _stockDialogOpen = False
            End Try
        End Function

        Private Shared Sub ReportStockFailure(title As String, source As String, product As ProductItem, ex As Exception)
            Dim label = If(product Is Nothing, "(no product)", $"{product.Sku} {product.Name}")
            ErrorLogService.LogException($"{source} — {label}", ex)
            AppDialogService.ShowError(
                $"Could not complete {title.ToLowerInvariant()} for {label}.{Environment.NewLine}{Environment.NewLine}{ErrorLogService.Describe(ex)}",
                title)
        End Sub

        Private Function TryBeginStockCommand(product As ProductItem) As Boolean
            If product Is Nothing OrElse _stockDialogOpen Then Return False
            Dim now = DateTime.UtcNow
            If (now - _lastStockCommandUtc).TotalMilliseconds < 300 Then Return False
            _lastStockCommandUtc = now
            Return True
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

        Private Sub ChooseImage()
            Dim picked = _images.PickImageFile()
            If picked Is Nothing Then Return
            _pendingSourcePath = picked
            _imageRemoved = False
            EditImagePath = picked
        End Sub

        Private Sub RemoveImage()
            _pendingSourcePath = Nothing
            _imageRemoved = True
            EditImagePath = String.Empty
        End Sub

        Private Sub ResetImageEdit(existingPath As String)
            _pendingSourcePath = Nothing
            _imageRemoved = False
            _originalImagePath = If(existingPath, String.Empty)
            EditImagePath = _originalImagePath
        End Sub

        Private Function CommitImage(id As String) As String
            If _pendingSourcePath IsNot Nothing Then
                Dim saved = _images.SaveImage(_pendingSourcePath, CatalogImageService.ProductsKind, id)
                If saved Is Nothing Then Return Nothing
                If Not String.IsNullOrWhiteSpace(_originalImagePath) AndAlso
                   Not _originalImagePath.Equals(saved, StringComparison.OrdinalIgnoreCase) Then
                    _images.DeleteImage(_originalImagePath)
                End If
                Return saved
            End If

            If _imageRemoved Then
                _images.DeleteImage(_originalImagePath)
                Return String.Empty
            End If

            Return _originalImagePath
        End Function
    End Class
End Namespace
