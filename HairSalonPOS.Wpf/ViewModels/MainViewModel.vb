Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class MainViewModel
        Inherits ViewModelBase

        Private _isLoggedIn As Boolean
        Private _currentView As ViewModelBase
        Private _statusText As String = String.Empty
        Private _lowStockCount As Integer
        Private _currentDateText As String = Date.Today.ToString("yyyy-MM-dd")

        Public Sub New()
            LoginViewModel = New LoginViewModel(AddressOf OnLoginSuccess)
            CashierViewModel = New CashierViewModel()
            InventoryViewModel = New InventoryViewModel()
            ReportsViewModel = New ReportsViewModel()
            CustomersViewModel = New CustomersViewModel()
            StaffViewModel = New StaffViewModel()
            DiscountsViewModel = New DiscountsViewModel()
            AppointmentsViewModel = New AppointmentsViewModel()
            SettingsViewModel = New SettingsViewModel()

            NavigateCashierCommand = New RelayCommand(AddressOf NavigateCashier, Function() IsLoggedIn)
            NavigateInventoryCommand = New RelayCommand(AddressOf NavigateInventory, Function() IsLoggedIn)
            NavigateReportsCommand = New RelayCommand(AddressOf NavigateReports, Function() IsLoggedIn)
            NavigateCustomersCommand = New RelayCommand(AddressOf NavigateCustomers, Function() IsLoggedIn)
            NavigateStaffCommand = New RelayCommand(AddressOf NavigateStaff, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateDiscountsCommand = New RelayCommand(AddressOf NavigateDiscounts, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateAppointmentsCommand = New RelayCommand(AddressOf NavigateAppointments, Function() IsLoggedIn)
            NavigateSettingsCommand = New RelayCommand(AddressOf NavigateSettings, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            LogoutCommand = New RelayCommand(AddressOf Logout, Function() IsLoggedIn)

            CurrentView = LoginViewModel
            AddHandler InMemoryDataStore.Instance.SaleCompleted, Sub() UpdateStatus()
            AddHandler InMemoryDataStore.Instance.InventoryChanged, Sub() UpdateStatus()
        End Sub

        Public Property LoginViewModel As LoginViewModel
        Public Property CashierViewModel As CashierViewModel
        Public Property InventoryViewModel As InventoryViewModel
        Public Property ReportsViewModel As ReportsViewModel
        Public Property CustomersViewModel As CustomersViewModel
        Public Property StaffViewModel As StaffViewModel
        Public Property DiscountsViewModel As DiscountsViewModel
        Public Property AppointmentsViewModel As AppointmentsViewModel
        Public Property SettingsViewModel As SettingsViewModel

        Public Property IsLoggedIn As Boolean
            Get
                Return _isLoggedIn
            End Get
            Set(value As Boolean)
                SetProperty(_isLoggedIn, value)
                NotifyNavCommands()
            End Set
        End Property

        Public Property CurrentView As ViewModelBase
            Get
                Return _currentView
            End Get
            Set(value As ViewModelBase)
                SetProperty(_currentView, value)
            End Set
        End Property

        Public Property StatusText As String
            Get
                Return _statusText
            End Get
            Set(value As String)
                SetProperty(_statusText, value)
            End Set
        End Property

        Public Property LowStockCount As Integer
            Get
                Return _lowStockCount
            End Get
            Set(value As Integer)
                SetProperty(_lowStockCount, value)
                OnPropertyChanged(NameOf(LowStockAlertText))
            End Set
        End Property

        Public Property CurrentDateText As String
            Get
                Return _currentDateText
            End Get
            Set(value As String)
                SetProperty(_currentDateText, value)
            End Set
        End Property

        Public ReadOnly Property LowStockAlertText As String
            Get
                If LowStockCount <= 0 Then Return String.Empty
                Return $"Low stock items: {LowStockCount} product(s) need restocking."
            End Get
        End Property

        Public ReadOnly Property CanViewAdminScreens As Boolean
            Get
                Return SessionContext.IsAdmin
            End Get
        End Property

        Public Property NavigateCashierCommand As RelayCommand
        Public Property NavigateInventoryCommand As RelayCommand
        Public Property NavigateReportsCommand As RelayCommand
        Public Property NavigateCustomersCommand As RelayCommand
        Public Property NavigateStaffCommand As RelayCommand
        Public Property NavigateDiscountsCommand As RelayCommand
        Public Property NavigateAppointmentsCommand As RelayCommand
        Public Property NavigateSettingsCommand As RelayCommand
        Public Property LogoutCommand As RelayCommand

        Private Sub OnLoginSuccess()
            IsLoggedIn = True
            UpdateStatus()
            NavigateCashier()
        End Sub

        Private Sub NavigateCashier()
            CurrentView = CashierViewModel
        End Sub

        Private Sub NavigateInventory()
            InventoryViewModel.LoadAll()
            CurrentView = InventoryViewModel
        End Sub

        Private Sub NavigateReports()
            ReportsViewModel.LoadReports()
            CurrentView = ReportsViewModel
        End Sub

        Private Sub NavigateCustomers()
            CustomersViewModel.LoadCustomers()
            CurrentView = CustomersViewModel
        End Sub

        Private Sub NavigateStaff()
            CurrentView = StaffViewModel
        End Sub

        Private Sub NavigateDiscounts()
            CurrentView = DiscountsViewModel
        End Sub

        Private Sub NavigateAppointments()
            AppointmentsViewModel.LoadAppointments()
            CurrentView = AppointmentsViewModel
        End Sub

        Private Sub NavigateSettings()
            SettingsViewModel.LoadFromSettings()
            CurrentView = SettingsViewModel
        End Sub

        Private Sub Logout()
            SessionContext.CurrentUser = Nothing
            IsLoggedIn = False
            CurrentView = LoginViewModel
            LoginViewModel.Username = String.Empty
            LoginViewModel.Password = String.Empty
            LoginViewModel.ErrorMessage = String.Empty
        End Sub

        Private Sub UpdateStatus()
            If SessionContext.CurrentUser IsNot Nothing Then
                StatusText = $"Logged in: {SessionContext.CurrentUser.FullName} | {SessionContext.CurrentUser.Role}"
            Else
                StatusText = String.Empty
            End If
            LowStockCount = InMemoryDataStore.Instance.GetLowStockCount()
            CurrentDateText = Date.Today.ToString("yyyy-MM-dd")
            OnPropertyChanged(NameOf(CanViewAdminScreens))
            NotifyNavCommands()
        End Sub

        Private Sub NotifyNavCommands()
            NavigateCashierCommand.NotifyCanExecuteChanged()
            NavigateInventoryCommand.NotifyCanExecuteChanged()
            NavigateReportsCommand.NotifyCanExecuteChanged()
            NavigateCustomersCommand.NotifyCanExecuteChanged()
            NavigateStaffCommand.NotifyCanExecuteChanged()
            NavigateDiscountsCommand.NotifyCanExecuteChanged()
            NavigateAppointmentsCommand.NotifyCanExecuteChanged()
            NavigateSettingsCommand.NotifyCanExecuteChanged()
            LogoutCommand.NotifyCanExecuteChanged()
        End Sub
    End Class
End Namespace
