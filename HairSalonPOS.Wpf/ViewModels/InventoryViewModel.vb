Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Namespace ViewModels
    Public Class InventoryViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _inventory As New InventoryService()

        Private _searchText As String = String.Empty
        Private _selectedProduct As ProductItem
        Private _showMovementLog As Boolean
        Private _editSku As String = String.Empty
        Private _editName As String = String.Empty
        Private _editBrand As String = String.Empty
        Private _editPrice As Decimal
        Private _editCost As Decimal
        Private _editQty As Integer
        Private _editReorder As Integer = 10
        Private _isEditMode As Boolean
        Private _statusMessage As String = String.Empty

        Public Sub New()
            Products = New ObservableCollection(Of ProductItem)()
            Movements = New ObservableCollection(Of StockMovement)()

            RefreshCommand = New RelayCommand(AddressOf LoadProducts)
            ShowProductsTabCommand = New RelayCommand(Sub() ShowMovementLog = False)
            ShowMovementLogCommand = New RelayCommand(Sub() ShowMovementLog = True)
            AddProductCommand = New RelayCommand(AddressOf BeginAddProduct)
            EditProductCommand = New RelayCommand(AddressOf BeginEditProduct, Function() SelectedProduct IsNot Nothing)
            EditProductRowCommand = New RelayCommand(Of ProductItem)(AddressOf BeginEditFromRow)
            SaveProductCommand = New RelayCommand(AddressOf SaveProduct)
            CancelEditCommand = New RelayCommand(AddressOf CancelEdit)
            DeleteProductCommand = New RelayCommand(Of ProductItem)(AddressOf DeleteProduct)
            ExportCommand = New RelayCommand(AddressOf ExportInventory)
            UpdateQtyCommand = New RelayCommand(Of ProductItem)(AddressOf PromptQtyUpdate)

            AddHandler _store.SaleCompleted, Sub() LoadAll()
            AddHandler _store.InventoryChanged, Sub() LoadMovements()
            LoadAll()
        End Sub

        Public Property Products As ObservableCollection(Of ProductItem)
        Public Property Movements As ObservableCollection(Of StockMovement)

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
                SetProperty(_selectedProduct, value)
                EditProductCommand.NotifyCanExecuteChanged()
            End Set
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

        Public ReadOnly Property FormTitle As String
            Get
                Return If(SelectedProduct Is Nothing, "Add product", "Edit product")
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

        Public Property EditCost As Decimal
            Get
                Return _editCost
            End Get
            Set(value As Decimal)
                SetProperty(_editCost, value)
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
        Public Property EditProductRowCommand As RelayCommand(Of ProductItem)
        Public Property SaveProductCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteProductCommand As RelayCommand(Of ProductItem)
        Public Property ExportCommand As RelayCommand
        Public Property UpdateQtyCommand As RelayCommand(Of ProductItem)

        Public Sub LoadProducts()
            Dim query = _store.Products.Where(Function(p) String.IsNullOrWhiteSpace(p.Category))
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                query = query.Where(Function(p) p.Name.ToLower().Contains(SearchText.ToLower()) OrElse p.Sku.ToLower().Contains(SearchText.ToLower()))
            End If
            Products = New ObservableCollection(Of ProductItem)(query.ToList())
            OnPropertyChanged(NameOf(Products))
        End Sub

        Public Sub LoadMovements()
            Movements = New ObservableCollection(Of StockMovement)(_store.StockMovements.OrderByDescending(Function(m) m.CreatedAt))
            OnPropertyChanged(NameOf(Movements))
        End Sub

        Public Sub LoadAll()
            LoadProducts()
            LoadMovements()
        End Sub

        Public Sub UpdateQtyInline(product As ProductItem, newQty As Integer)
            Try
                _inventory.UpdateStockInline(product.Sku, newQty, SessionContext.CurrentUser.FullName)
                StatusMessage = $"Updated {product.Name} qty to {newQty}."
            Catch ex As Exception
                StatusMessage = ex.Message
            End Try
        End Sub

        Private Sub BeginAddProduct()
            SelectedProduct = Nothing
            IsEditMode = True
            EditSku = $"P{(_store.Products.Count + 1):D3}"
            EditName = String.Empty
            EditBrand = String.Empty
            EditPrice = 0D
            EditCost = 0D
            EditQty = 0
            EditReorder = 10
            OnPropertyChanged(NameOf(FormTitle))
        End Sub

        Private Sub BeginEditFromRow(product As ProductItem)
            SelectedProduct = product
            BeginEditProduct()
        End Sub

        Private Sub BeginEditProduct()
            If SelectedProduct Is Nothing Then Return
            IsEditMode = True
            EditSku = SelectedProduct.Sku
            EditName = SelectedProduct.Name
            EditBrand = SelectedProduct.Brand
            EditPrice = SelectedProduct.Price
            EditCost = SelectedProduct.Cost
            EditQty = SelectedProduct.StockOnHand
            EditReorder = SelectedProduct.ReorderLevel
            OnPropertyChanged(NameOf(FormTitle))
        End Sub

        Private Sub SaveProduct()
            Try
                Dim isNew = SelectedProduct Is Nothing
                Dim product As New ProductItem With {
                    .Sku = EditSku.Trim(),
                    .Name = EditName.Trim(),
                    .Brand = EditBrand.Trim(),
                    .Price = EditPrice,
                    .Cost = EditCost,
                    .StockOnHand = EditQty,
                    .ReorderLevel = EditReorder
                }
                _inventory.SaveProduct(product, isNew, SessionContext.CurrentUser.FullName)
                IsEditMode = False
                StatusMessage = "Product saved."
                LoadAll()
            Catch ex As Exception
                StatusMessage = ex.Message
            End Try
        End Sub

        Private Sub CancelEdit()
            IsEditMode = False
        End Sub

        Private Sub DeleteProduct(product As ProductItem)
            If product Is Nothing Then Return
            If Not SessionContext.IsAdmin Then
                StatusMessage = "Only Admin can manage inventory."
                Return
            End If
            Dim confirm = System.Windows.MessageBox.Show(
                $"Delete product '{product.Name}'?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

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
            If ExportService.ExportInventoryPdf(_store.Products, $"{salonName} Inventory") Then
                StatusMessage = "Inventory exported as PDF."
            End If
        End Sub

        Private Sub PromptQtyUpdate(product As ProductItem)
            If product Is Nothing Then Return
            Dim input = Microsoft.VisualBasic.Interaction.InputBox($"New quantity for {product.Name}:", "Update Qty", product.StockOnHand.ToString())
            If String.IsNullOrWhiteSpace(input) Then Return
            Dim qty As Integer
            If Integer.TryParse(input, qty) Then UpdateQtyInline(product, qty)
        End Sub
    End Class
End Namespace
