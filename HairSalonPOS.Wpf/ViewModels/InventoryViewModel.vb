Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports HairSalonPOS.Wpf.Helpers
Namespace ViewModels
    Public Class InventoryViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _inventory As New InventoryService()
        Private ReadOnly _images As CatalogImageService = CatalogImageService.Instance

        Private _searchText As String = String.Empty
        Private _selectedProduct As ProductItem
        Private _showMovementLog As Boolean
        Private _showLowStockOnly As Boolean
        Private _editSku As String = String.Empty
        Private _editName As String = String.Empty
        Private _editBrand As String = String.Empty
        Private _editPrice As Decimal
        Private _editQty As Integer
        Private _editReorder As Integer = 10
        Private _editImagePath As String = String.Empty
        Private _pendingSourcePath As String
        Private _originalImagePath As String = String.Empty
        Private _imageRemoved As Boolean
        Private _isEditMode As Boolean
        Private _isAdding As Boolean
        Private _statusMessage As String = String.Empty
        Private _suppressProductPopup As Boolean
        Private _stockDialogOpen As Boolean
        Private _lastStockCommandUtc As DateTime = DateTime.MinValue

        Public Sub New()
            Products = New ObservableCollection(Of ProductItem)()
            Movements = New ObservableCollection(Of StockMovement)()
            ProductMovements = New ObservableCollection(Of StockMovement)()

            RefreshCommand = New RelayCommand(AddressOf LoadAll)
            ShowProductsTabCommand = New RelayCommand(Sub() ShowMovementLog = False)
            ShowMovementLogCommand = New RelayCommand(Sub() ShowMovementLog = True)
            AddProductCommand = New RelayCommand(AddressOf BeginAddProduct)
            EditProductCommand = New RelayCommand(AddressOf BeginEditProduct, Function() SelectedProduct IsNot Nothing)
            SaveProductCommand = New RelayCommand(AddressOf SaveProduct)
            CancelEditCommand = New RelayCommand(AddressOf CancelEdit)
            DeleteProductCommand = New RelayCommand(AddressOf DeleteSelected)
            ExportCommand = New RelayCommand(AddressOf ExportInventory)
            ChooseImageCommand = New RelayCommand(AddressOf ChooseImage)
            RemoveImageCommand = New RelayCommand(AddressOf RemoveImage)
            StockInCommand = New RelayCommand(AddressOf StockInSelected, AddressOf CanRunStockCommand)
            StockOutCommand = New RelayCommand(AddressOf StockOutSelected, AddressOf CanRunStockCommand)
            CreateOrderCommand = New RelayCommand(AddressOf CreateOrderSelected, AddressOf CanRunCreateOrderCommand)
            ClearLowStockFilterCommand = New RelayCommand(AddressOf ClearLowStockFilter, Function() ShowLowStockOnly)

            AddHandler _store.SaleCompleted, Sub() LoadAll()
            AddHandler _store.InventoryChanged, Sub() LoadMovements()
            LoadAll()
        End Sub

        Public Property Products As ObservableCollection(Of ProductItem)
        Public Property Movements As ObservableCollection(Of StockMovement)
        Public Property ProductMovements As ObservableCollection(Of StockMovement)

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
                    NotifyStockCommands()
                    OnPropertyChanged(NameOf(HasSelectedProduct))
                    OnPropertyChanged(NameOf(HasProductMovements))
                    RefreshProductMovements()
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

        Public Property ShowMovementLog As Boolean
            Get
                Return _showMovementLog
            End Get
            Set(value As Boolean)
                SetProperty(_showMovementLog, value)
                OnPropertyChanged(NameOf(ShowProductsTab))
            End Set
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

        Public ReadOnly Property ShowProductsTab As Boolean
            Get
                Return Not ShowMovementLog
            End Get
        End Property

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                SetProperty(_isEditMode, value)
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
                Dim category = If(SelectedProduct?.Category, String.Empty)
                Return ProductPlaceholderIcons.ResolveFromText($"{EditName} {EditBrand} {category}")
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
        Public Property ShowMovementLogCommand As RelayCommand
        Public Property AddProductCommand As RelayCommand
        Public Property EditProductCommand As RelayCommand
        Public Property SaveProductCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteProductCommand As RelayCommand
        Public Property ExportCommand As RelayCommand
        Public Property ChooseImageCommand As RelayCommand
        Public Property RemoveImageCommand As RelayCommand
        Public Property StockInCommand As RelayCommand
        Public Property StockOutCommand As RelayCommand
        Public Property CreateOrderCommand As RelayCommand
        Public Property ClearLowStockFilterCommand As RelayCommand

        Public Sub ApplyLowStockFilter()
            ShowMovementLog = False
            ShowLowStockOnly = True
            StatusMessage = "Showing products that are low or out of stock."
        End Sub

        Public Sub OpenProductDetailPopup()
            If SelectedProduct Is Nothing OrElse IsEditMode OrElse Not ShowProductsTab Then Return
            AppDialogService.PromptProductDetail(Me)
        End Sub

        Public Sub RefreshSelectedProductFromStore()
            If SelectedProduct Is Nothing Then Return
            Dim sku = SelectedProduct.Sku
            _suppressProductPopup = True
            Try
                Dim updated = _store.Products.FirstOrDefault(Function(p) p.Sku = sku)
                If updated IsNot Nothing Then
                    SelectedProduct = updated
                End If
            Finally
                _suppressProductPopup = False
            End Try
            RefreshProductMovements()
        End Sub

        Public Function RunStockInFromPopup() As Boolean
            Return CompleteStockIn(SelectedProduct)
        End Function

        Public Function RunStockOutFromPopup() As Boolean
            Return CompleteStockOut(SelectedProduct)
        End Function

        Public Function RunCreateOrderFromPopup() As Boolean
            Dim product = SelectedProduct
            If product Is Nothing OrElse Not product.ShowStockWarning Then Return False
            Return CompleteStockIn(product, product.SuggestedOrderQty)
        End Function

        Public Sub BeginEditFromPopup()
            BeginEditProduct()
        End Sub

        Public Sub DeleteFromPopup()
            DeleteSelected()
        End Sub

        Private Function CanRunStockCommand() As Boolean
            Return SelectedProduct IsNot Nothing AndAlso Not _stockDialogOpen
        End Function

        Private Function CanRunCreateOrderCommand() As Boolean
            Return CanRunStockCommand() AndAlso SelectedProduct.ShowStockWarning
        End Function

        Private Function TryBeginStockCommand() As Boolean
            If Not CanRunStockCommand() Then Return False
            Dim now = DateTime.UtcNow
            If (now - _lastStockCommandUtc).TotalMilliseconds < 300 Then Return False
            _lastStockCommandUtc = now
            Return True
        End Function

        Private Sub NotifyStockCommands()
            StockInCommand.NotifyCanExecuteChanged()
            StockOutCommand.NotifyCanExecuteChanged()
            CreateOrderCommand.NotifyCanExecuteChanged()
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
            _suppressProductPopup = True
            Try
                SelectedProduct = match
            Finally
                _suppressProductPopup = False
            End Try
            NotifyStockCommands()
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
            _isAdding = True
            SelectedProduct = Nothing
            IsEditMode = True
            EditSku = $"P{(_store.Products.Count + 1):D3}"
            EditName = String.Empty
            EditBrand = String.Empty
            EditPrice = 0D
            EditQty = 0
            EditReorder = 10
            ResetImageEdit(String.Empty)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(ShowEditQty))
        End Sub

        Private Sub BeginEditProduct()
            If SelectedProduct Is Nothing Then Return
            _isAdding = False
            IsEditMode = True
            EditSku = SelectedProduct.Sku
            EditName = SelectedProduct.Name
            EditBrand = SelectedProduct.Brand
            EditPrice = SelectedProduct.Price
            EditQty = SelectedProduct.StockOnHand
            EditReorder = SelectedProduct.ReorderLevel
            ResetImageEdit(SelectedProduct.ImagePath)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(ShowEditQty))
        End Sub

        Private Sub SaveProduct()
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
                    .Category = If(SelectedProduct?.Category, String.Empty),
                    .SubCategory = If(SelectedProduct?.SubCategory, String.Empty),
                    .ImagePath = If(imagePath, String.Empty)
                }
                _inventory.SaveProduct(product, _isAdding, SessionContext.CurrentUser.FullName)
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
            _suppressProductPopup = True
            Try
                If SelectedProduct Is Nothing AndAlso Products.Count > 0 Then
                    SelectedProduct = Products(0)
                End If
            Finally
                _suppressProductPopup = False
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

        Private Sub StockInSelected()
            CompleteStockIn(SelectedProduct)
        End Sub

        Private Sub StockOutSelected()
            CompleteStockOut(SelectedProduct)
        End Sub

        Private Function CompleteStockIn(product As ProductItem, Optional suggestedQty As Integer = 1) As Boolean
            If product Is Nothing OrElse Not TryBeginStockCommand() Then Return False
            _stockDialogOpen = True
            NotifyStockCommands()
            Try
                Dim prompt = AppDialogService.PromptStockMovement(product, True, initialQty:=suggestedQty)
                If prompt Is Nothing Then Return False
                _inventory.StockIn(product.Sku, prompt.Quantity, SessionContext.CurrentUser.FullName, prompt.CombinedNotes)
                StatusMessage = $"Stocked in {prompt.Quantity} of {product.Name}."
                LoadAll()
                Return True
            Catch ex As Exception
                StatusMessage = ex.Message
                Return False
            Finally
                _stockDialogOpen = False
                NotifyStockCommands()
            End Try
        End Function

        Private Sub CreateOrderSelected()
            Dim product = SelectedProduct
            If product Is Nothing OrElse Not product.ShowStockWarning Then Return
            CompleteStockIn(product, product.SuggestedOrderQty)
        End Sub

        Private Function CompleteStockOut(product As ProductItem) As Boolean
            If product Is Nothing OrElse Not TryBeginStockCommand() Then Return False
            _stockDialogOpen = True
            NotifyStockCommands()
            Try
                Dim prompt = AppDialogService.PromptStockMovement(product, False)
                If prompt Is Nothing Then Return False
                _inventory.StockOut(product.Sku, prompt.Quantity, SessionContext.CurrentUser.FullName, prompt.CombinedNotes)
                StatusMessage = $"Stocked out {prompt.Quantity} of {product.Name}."
                LoadAll()
                Return True
            Catch ex As Exception
                StatusMessage = ex.Message
                Return False
            Finally
                _stockDialogOpen = False
                NotifyStockCommands()
            End Try
        End Function

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
