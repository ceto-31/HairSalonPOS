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
        Private _statusMessage As String = String.Empty

        Public Sub New()
            Customers = New ObservableCollection(Of CustomerItem)()
            AddCustomerCommand = New RelayCommand(AddressOf BeginAdd)
            SaveCustomerCommand = New RelayCommand(AddressOf SaveCustomer)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
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
        Public Property SaveCustomerCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand

        Public Sub LoadCustomers()
            Dim query = _store.Customers.Where(Function(c) c.Name <> "Walk-in")
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                query = query.Where(Function(c) c.Name.ToLower().Contains(SearchText.ToLower()) OrElse c.Phone.Contains(SearchText))
            End If
            Customers = New ObservableCollection(Of CustomerItem)(query.OrderByDescending(Function(c) c.VisitCount))
            OnPropertyChanged(NameOf(Customers))
        End Sub

        Private Sub BeginAdd()
            IsEditMode = True
            EditName = String.Empty
            EditPhone = String.Empty
        End Sub

        Private Sub SaveCustomer()
            If String.IsNullOrWhiteSpace(EditName) Then
                StatusMessage = "Name is required."
                Return
            End If
            _store.Customers.Add(New CustomerItem With {
                .CustomerId = _store.Customers.Count + 1,
                .Name = EditName.Trim(),
                .Phone = EditPhone.Trim(),
                .VisitCount = 0,
                .LoyaltyPoints = 0
            })
            IsEditMode = False
            StatusMessage = "Customer added."
            LoadCustomers()
        End Sub
    End Class
End Namespace
