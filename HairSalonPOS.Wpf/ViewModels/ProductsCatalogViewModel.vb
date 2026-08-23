Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    ''' <summary>Master Files product catalog (name/price/category). No stock quantity editing.</summary>
    Public Class ProductsCatalogViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _images As CatalogImageService = CatalogImageService.Instance

        Private _showArchived As Boolean
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingSku As String = String.Empty
        Private _statusMessage As String = String.Empty

        Private _editName As String = String.Empty
        Private _editBrand As String = String.Empty
        Private _editPrice As Decimal
        Private _editCategory As String = String.Empty
        Private _editSubCategory As String = String.Empty
        Private _editImagePath As String = String.Empty
        Private _pendingSourcePath As String
        Private _originalImagePath As String = String.Empty
        Private _imageRemoved As Boolean

        Public Sub New()
            Products = New ObservableCollection(Of ProductItem)()
            EditCategoryOptions = New ObservableCollection(Of String)()
            EditSubCategoryOptions = New ObservableCollection(Of String)()

            AddProductCommand = New RelayCommand(AddressOf BeginAdd)
            EditProductCommand = New RelayCommand(Of ProductItem)(AddressOf BeginEdit)
            SaveProductCommand = New RelayCommand(AddressOf SaveProduct)
            CancelEditCommand = New RelayCommand(AddressOf CancelEdit)
            DeleteProductCommand = New RelayCommand(Of ProductItem)(AddressOf DeleteProduct)
            ArchiveProductCommand = New RelayCommand(Of ProductItem)(AddressOf ArchiveProduct)
            UnarchiveProductCommand = New RelayCommand(Of ProductItem)(AddressOf UnarchiveProduct)
            ToggleShowArchivedCommand = New RelayCommand(AddressOf ToggleShowArchived)
            ChooseImageCommand = New RelayCommand(AddressOf ChooseImage)
            RemoveImageCommand = New RelayCommand(AddressOf RemoveImage)

            LoadProducts()
        End Sub

        Public Sub LoadFromStore()
            LoadProducts()
            StatusMessage = String.Empty
        End Sub

        Public Property Products As ObservableCollection(Of ProductItem)
        Public Property EditCategoryOptions As ObservableCollection(Of String)
        Public Property EditSubCategoryOptions As ObservableCollection(Of String)

        Public Property ShowArchived As Boolean
            Get
                Return _showArchived
            End Get
            Set(value As Boolean)
                If SetProperty(_showArchived, value) Then
                    LoadProducts()
                    OnPropertyChanged(NameOf(ShowArchivedLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property ShowArchivedLabel As String
            Get
                Return If(ShowArchived, "Hide archived", "Show archived")
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

        Public ReadOnly Property FormTitle As String
            Get
                Return If(_isAdding, "Add product", "Edit product")
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

        Public Property EditBrand As String
            Get
                Return _editBrand
            End Get
            Set(value As String)
                SetProperty(_editBrand, value)
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

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property AddProductCommand As RelayCommand
        Public Property EditProductCommand As RelayCommand(Of ProductItem)
        Public Property SaveProductCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteProductCommand As RelayCommand(Of ProductItem)
        Public Property ArchiveProductCommand As RelayCommand(Of ProductItem)
        Public Property UnarchiveProductCommand As RelayCommand(Of ProductItem)
        Public Property ToggleShowArchivedCommand As RelayCommand
        Public Property ChooseImageCommand As RelayCommand
        Public Property RemoveImageCommand As RelayCommand

        Private Sub ToggleShowArchived()
            ShowArchived = Not ShowArchived
        End Sub

        Private Sub LoadProducts()
            Dim query = _store.Products.AsEnumerable()
            If Not ShowArchived Then
                query = query.Where(Function(p) p.IsActive)
            End If
            Products = New ObservableCollection(Of ProductItem)(query.OrderBy(Function(p) p.Name))
            OnPropertyChanged(NameOf(Products))
        End Sub

        Private Sub BeginAdd()
            RefreshEditCategoryOptions()
            If EditCategoryOptions.Count = 0 Then
                StatusMessage = "Add an active category first."
                Return
            End If
            _isAdding = True
            _editingSku = String.Empty
            EditName = String.Empty
            EditBrand = String.Empty
            EditPrice = 0D
            EditCategory = EditCategoryOptions.First()
            ResetImageEdit(String.Empty)
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(item As ProductItem)
            If item Is Nothing Then Return
            RefreshEditCategoryOptions()
            _isAdding = False
            _editingSku = item.Sku
            EditName = item.Name
            EditBrand = item.Brand
            EditPrice = item.Price
            EditCategory = item.Category
            EditSubCategory = item.SubCategory
            ResetImageEdit(item.ImagePath)
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub CancelEdit()
            IsEditMode = False
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

            If _isAdding Then
                Dim sku = ProductSkuService.NextProductSku(_store.Products)
                Dim imagePath = CommitImage(sku)
                If imagePath Is Nothing AndAlso _pendingSourcePath IsNot Nothing Then
                    StatusMessage = "Could not save the photo."
                    Return
                End If
                Dim product As New ProductItem With {
                    .Sku = sku,
                    .Name = EditName.Trim(),
                    .Brand = If(EditBrand, String.Empty).Trim(),
                    .Price = EditPrice,
                    .Cost = 0D,
                    .Category = node.Name,
                    .SubCategory = subCat,
                    .StockOnHand = 0,
                    .ReorderLevel = 10,
                    .IsActive = True,
                    .ImagePath = If(imagePath, String.Empty)
                }
                product.EnsureDefaults()
                _store.Products.Add(product)
                StatusMessage = "Product added."
            Else
                Dim existing = _store.Products.FirstOrDefault(Function(p) p.Sku = _editingSku)
                If existing Is Nothing Then
                    StatusMessage = "Product not found."
                    Return
                End If
                Dim imagePath = CommitImage(existing.Sku)
                If imagePath Is Nothing AndAlso _pendingSourcePath IsNot Nothing Then
                    StatusMessage = "Could not save the photo."
                    Return
                End If
                existing.Name = EditName.Trim()
                existing.Brand = If(EditBrand, String.Empty).Trim()
                existing.Price = EditPrice
                existing.Category = node.Name
                existing.SubCategory = subCat
                existing.ImagePath = If(imagePath, String.Empty)
                StatusMessage = "Product updated."
            End If

            _store.PersistCatalog()
            IsEditMode = False
            LoadProducts()
        End Sub

        Private Sub DeleteProduct(item As ProductItem)
            If item Is Nothing Then Return
            If Not AppDialogService.ConfirmDelete(item.Name) Then Return
            _images.DeleteImage(item.ImagePath)
            _store.Products.Remove(item)
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} deleted."
            LoadProducts()
        End Sub

        Private Sub ArchiveProduct(item As ProductItem)
            If item Is Nothing OrElse Not item.IsActive Then Return
            item.IsActive = False
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} archived."
            LoadProducts()
        End Sub

        Private Sub UnarchiveProduct(item As ProductItem)
            If item Is Nothing OrElse item.IsActive Then Return
            item.IsActive = True
            _store.PersistCatalog()
            StatusMessage = $"{item.Name} restored."
            LoadProducts()
        End Sub

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
