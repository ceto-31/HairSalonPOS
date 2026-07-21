Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class CustomersViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private _searchText As String = String.Empty
        Private _selectedCustomer As CustomerItem
        Private _editName As String = String.Empty
        Private _editPhone As String = String.Empty
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingCustomerId As Integer
        Private _statusMessage As String = String.Empty

        Public Sub New()
            Customers = New ObservableCollection(Of CustomerItem)()
            AddCustomerCommand = New RelayCommand(AddressOf BeginAdd)
            EditCustomerCommand = New RelayCommand(Of CustomerItem)(AddressOf BeginEdit)
            SaveCustomerCommand = New RelayCommand(AddressOf SaveCustomer)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteCustomerCommand = New RelayCommand(Of CustomerItem)(AddressOf DeleteCustomer)
            LoadCustomers()
        End Sub

        Public Property Customers As ObservableCollection(Of CustomerItem)

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                SetProperty(_searchText, value)
                LoadCustomers()
            End Set
        End Property

        Public Property SelectedCustomer As CustomerItem
            Get
                Return _selectedCustomer
            End Get
            Set(value As CustomerItem)
                SetProperty(_selectedCustomer, value)
            End Set
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
                Return If(_isAdding, "Add customer", "Edit customer")
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

        Public Property EditPhone As String
            Get
                Return _editPhone
            End Get
            Set(value As String)
                SetProperty(_editPhone, value)
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

        Public Property AddCustomerCommand As RelayCommand
        Public Property EditCustomerCommand As RelayCommand(Of CustomerItem)
        Public Property SaveCustomerCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteCustomerCommand As RelayCommand(Of CustomerItem)

        Public Sub LoadCustomers()
            Dim query = _store.Customers.Where(Function(c) c.Name <> "Walk-in")
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                query = query.Where(Function(c) c.Name.ToLower().Contains(SearchText.ToLower()) OrElse c.Phone.Contains(SearchText))
            End If
            Customers = New ObservableCollection(Of CustomerItem)(query.OrderByDescending(Function(c) c.VisitCount))
            OnPropertyChanged(NameOf(Customers))
        End Sub

        Private Sub BeginAdd()
            _isAdding = True
            _editingCustomerId = 0
            EditName = String.Empty
            EditPhone = String.Empty
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(customer As CustomerItem)
            If customer Is Nothing Then Return
            _isAdding = False
            _editingCustomerId = customer.CustomerId
            EditName = customer.Name
            EditPhone = customer.Phone
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub SaveCustomer()
            If String.IsNullOrWhiteSpace(EditName) Then
                StatusMessage = "Name is required."
                Return
            End If

            If _isAdding Then
                _store.Customers.Add(New CustomerItem With {
                    .CustomerId = If(_store.Customers.Count = 0, 1, _store.Customers.Max(Function(c) c.CustomerId) + 1),
                    .Name = EditName.Trim(),
                    .Phone = EditPhone.Trim(),
                    .VisitCount = 0,
                    .LoyaltyPoints = 0
                })
                StatusMessage = "Customer added."
            Else
                Dim existing = _store.Customers.FirstOrDefault(Function(c) c.CustomerId = _editingCustomerId)
                If existing Is Nothing Then
                    StatusMessage = "Customer not found."
                    Return
                End If
                existing.Name = EditName.Trim()
                existing.Phone = EditPhone.Trim()
                StatusMessage = "Customer updated."
            End If

            IsEditMode = False
            LoadCustomers()
            _store.RaiseCustomersChanged()
        End Sub

        Private Sub DeleteCustomer(customer As CustomerItem)
            If customer Is Nothing Then Return
            Dim confirm = System.Windows.MessageBox.Show(
                $"Delete customer '{customer.Name}'?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

            _store.Customers.Remove(customer)
            StatusMessage = $"{customer.Name} deleted."
            LoadCustomers()
            _store.RaiseCustomersChanged()
        End Sub
    End Class
End Namespace
