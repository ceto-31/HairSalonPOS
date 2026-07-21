Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class MainViewModel
        Inherits ViewModelBase

        Private Const BusinessHoursDisplay As String = "9am - 5pm"

        Private _isLoggedIn As Boolean
        Private _currentView As ViewModelBase
        Private _lowStockCount As Integer
        Private _appointmentCountToday As Integer
        Private _currentDateText As String = Date.Today.ToString("yyyy-MM-dd")
        Private _greetingText As String = String.Empty
        Private _headerDateTimeText As String = String.Empty
        Private _currentNavKey As String = String.Empty
        Private _isDrawerOpen As Boolean
        Private _isDarkMode As Boolean

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
            NavigateInventoryCommand = New RelayCommand(AddressOf NavigateInventory, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateReportsCommand = New RelayCommand(AddressOf NavigateReports, Function() IsLoggedIn)
            NavigateCustomersCommand = New RelayCommand(AddressOf NavigateCustomers, Function() IsLoggedIn)
            NavigateStaffCommand = New RelayCommand(AddressOf NavigateStaff, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateDiscountsCommand = New RelayCommand(AddressOf NavigateDiscounts, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateAppointmentsCommand = New RelayCommand(AddressOf NavigateAppointments, Function() IsLoggedIn)
            NavigateSettingsCommand = New RelayCommand(AddressOf NavigateSettings, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            LogoutCommand = New RelayCommand(AddressOf Logout, Function() IsLoggedIn)
            ToggleDrawerCommand = New RelayCommand(AddressOf ToggleDrawer, Function() IsLoggedIn)
            CloseDrawerCommand = New RelayCommand(AddressOf CloseDrawer, Function() IsLoggedIn)

            _isDarkMode = AppSettingsService.Instance.Settings.IsDarkMode
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
                OnPropertyChanged(NameOf(ContentMargin))
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

        Public Property LowStockCount As Integer
            Get
                Return _lowStockCount
            End Get
            Set(value As Integer)
                SetProperty(_lowStockCount, value)
                OnPropertyChanged(NameOf(LowStockAlertText))
                OnPropertyChanged(NameOf(LowStockHeaderText))
            End Set
        End Property

        Public Property AppointmentCountToday As Integer
            Get
                Return _appointmentCountToday
            End Get
            Set(value As Integer)
                SetProperty(_appointmentCountToday, value)
                OnPropertyChanged(NameOf(AppointmentsHeaderText))
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

        Public Property GreetingText As String
            Get
                Return _greetingText
            End Get
            Set(value As String)
                SetProperty(_greetingText, value)
            End Set
        End Property

        Public Property HeaderDateTimeText As String
            Get
                Return _headerDateTimeText
            End Get
            Set(value As String)
                SetProperty(_headerDateTimeText, value)
            End Set
        End Property

        Public Property CurrentNavKey As String
            Get
                Return _currentNavKey
            End Get
            Set(value As String)
                SetProperty(_currentNavKey, value)
            End Set
        End Property

        Public Property IsDrawerOpen As Boolean
            Get
                Return _isDrawerOpen
            End Get
            Set(value As Boolean)
                SetProperty(_isDrawerOpen, value)
            End Set
        End Property

        Public Property IsDarkMode As Boolean
            Get
                Return _isDarkMode
            End Get
            Set(value As Boolean)
                If Not SetProperty(_isDarkMode, value) Then Return
                ThemeService.Apply(value)
                Dim settings = AppSettingsService.Instance.Settings
                settings.IsDarkMode = value
                AppSettingsService.Instance.Save(settings)
                OnPropertyChanged(NameOf(ThemeToggleLabel))
            End Set
        End Property

        Public ReadOnly Property ThemeToggleLabel As String
            Get
                Return If(IsDarkMode, "Light mode", "Dark mode")
            End Get
        End Property

        Public ReadOnly Property BusinessHoursText As String
            Get
                Return BusinessHoursDisplay
            End Get
        End Property

        Public ReadOnly Property BusinessHoursHeaderText As String
            Get
                Return $"Business Hours : {BusinessHoursDisplay}"
            End Get
        End Property

        Public ReadOnly Property AppointmentsHeaderText As String
            Get
                Return $"Appointments : {AppointmentCountToday}"
            End Get
        End Property

        Public ReadOnly Property LowStockHeaderText As String
            Get
                Return $"Low stock : {LowStockCount}"
            End Get
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

        Public ReadOnly Property ContentMargin As Thickness
            Get
                Return If(IsLoggedIn, New Thickness(24), New Thickness(0))
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
        Public Property ToggleDrawerCommand As RelayCommand
        Public Property CloseDrawerCommand As RelayCommand

        Private Sub OnLoginSuccess()
            IsLoggedIn = True
            UpdateStatus()
            NavigateCashier()
        End Sub

        Private Sub NavigateCashier()
            CashierViewModel.RefreshLookups()
            CurrentView = CashierViewModel
            CurrentNavKey = "Cashier"
            CloseDrawer()
        End Sub

        Private Sub NavigateInventory()
            InventoryViewModel.LoadAll()
            CurrentView = InventoryViewModel
            CurrentNavKey = "Inventory"
            CloseDrawer()
        End Sub

        Private Sub NavigateReports()
            ReportsViewModel.LoadReports()
            CurrentView = ReportsViewModel
            CurrentNavKey = "Reports"
            CloseDrawer()
        End Sub

        Private Sub NavigateCustomers()
            CustomersViewModel.LoadCustomers()
            CurrentView = CustomersViewModel
            CurrentNavKey = "Customers"
            CloseDrawer()
        End Sub

        Private Sub NavigateStaff()
            CurrentView = StaffViewModel
            CurrentNavKey = "Staff"
            CloseDrawer()
        End Sub

        Private Sub NavigateDiscounts()
            CurrentView = DiscountsViewModel
            CurrentNavKey = "Discounts"
            CloseDrawer()
        End Sub

        Private Sub NavigateAppointments()
            AppointmentsViewModel.RefreshStaffList()
            AppointmentsViewModel.LoadAppointments()
            CurrentView = AppointmentsViewModel
            CurrentNavKey = "Appointments"
            CloseDrawer()
        End Sub

        Private Sub NavigateSettings()
            SettingsViewModel.LoadFromSettings()
            CurrentView = SettingsViewModel
            CurrentNavKey = "Settings"
            CloseDrawer()
        End Sub

        Private Sub Logout()
            CloseDrawer()
            SessionContext.CurrentUser = Nothing
            IsLoggedIn = False
            CurrentNavKey = String.Empty
            CurrentView = LoginViewModel
            LoginViewModel.Username = String.Empty
            LoginViewModel.Password = String.Empty
            LoginViewModel.ErrorMessage = String.Empty
            GreetingText = String.Empty
        End Sub

        Private Sub ToggleDrawer()
            IsDrawerOpen = Not IsDrawerOpen
        End Sub

        Private Sub CloseDrawer()
            IsDrawerOpen = False
        End Sub

        Private Sub UpdateStatus()
            LowStockCount = InMemoryDataStore.Instance.GetLowStockCount()
            AppointmentCountToday = InMemoryDataStore.Instance.Appointments.Where(Function(a) a.StartTime.Date = Date.Today).Count()
            CurrentDateText = Date.Today.ToString("yyyy-MM-dd")
            RefreshHeaderTexts()
            OnPropertyChanged(NameOf(CanViewAdminScreens))
            NotifyNavCommands()
        End Sub

        Private Sub RefreshHeaderTexts()
            Dim now = DateTime.Now
            Dim name = If(SessionContext.CurrentUser?.FullName, "User")
            Dim role = If(SessionContext.CurrentUser?.Role, String.Empty)
            Dim displayName = If(String.IsNullOrWhiteSpace(role), name, If(role.Equals("Admin", StringComparison.OrdinalIgnoreCase), "Admin", name))

            Dim hour = now.Hour
            Dim period As String
            If hour < 12 Then
                period = "Good Morning"
            ElseIf hour < 17 Then
                period = "Good Afternoon"
            Else
                period = "Good Evening"
            End If

            GreetingText = $"{period}, {displayName}!"
            HeaderDateTimeText = now.ToString("ddd, MMMM d, yyyy | h:mmtt")
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
            ToggleDrawerCommand.NotifyCanExecuteChanged()
            CloseDrawerCommand.NotifyCanExecuteChanged()
        End Sub
    End Class
End Namespace
