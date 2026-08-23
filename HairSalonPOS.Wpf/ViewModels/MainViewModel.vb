Imports System.Windows
Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class MainViewModel
        Inherits ViewModelBase

        Private Const DrawerWidth As Double = 260

        Private ReadOnly _clockTimer As DispatcherTimer
        Private _lastAppointmentStatusRefresh As DateTime = DateTime.MinValue
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
            DashboardViewModel = New DashboardViewModel()
            CashierViewModel = New CashierViewModel()
            TransactionsViewModel = New TransactionsViewModel()
            InventoryViewModel = New InventoryViewModel()
            ReportsViewModel = New ReportsViewModel()
            MasterFilesViewModel = New MasterFilesViewModel()
            AppointmentsViewModel = New AppointmentsViewModel(AddressOf OpenAppointmentAtPointOfSale)
            SettingsViewModel = New SettingsViewModel()
            DashboardViewModel.BindNavigation(
                AddressOf NavigateCashier,
                AddressOf OpenNewAppointment,
                AddressOf NavigateAppointments,
                AddressOf NavigateTransactions,
                AddressOf NavigateInventory,
                AddressOf NavigateReports,
                AddressOf OpenServices)

            NavigateDashboardCommand = New RelayCommand(AddressOf NavigateDashboard, Function() IsLoggedIn)
            NavigateCashierCommand = New RelayCommand(AddressOf NavigateCashier, Function() IsLoggedIn)
            NavigateTransactionsCommand = New RelayCommand(AddressOf NavigateTransactions, Function() IsLoggedIn)
            NavigateInventoryCommand = New RelayCommand(AddressOf NavigateInventory, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            FilterLowStockCommand = New RelayCommand(AddressOf NavigateInventoryLowStock, Function() IsLoggedIn AndAlso SessionContext.IsAdmin AndAlso LowStockCount > 0)
            NavigateReportsCommand = New RelayCommand(AddressOf NavigateReports, Function() IsLoggedIn)
            NavigateMasterFilesCommand = New RelayCommand(AddressOf NavigateMasterFiles, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            NavigateAppointmentsCommand = New RelayCommand(AddressOf NavigateAppointments, Function() IsLoggedIn)
            NavigateSettingsCommand = New RelayCommand(AddressOf NavigateSettings, Function() IsLoggedIn AndAlso SessionContext.IsAdmin)
            LogoutCommand = New RelayCommand(AddressOf Logout, Function() IsLoggedIn)
            ToggleDrawerCommand = New RelayCommand(AddressOf ToggleDrawer, Function() IsLoggedIn)
            CloseDrawerCommand = New RelayCommand(AddressOf CloseDrawer, Function() IsLoggedIn)

            _clockTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}
            AddHandler _clockTimer.Tick, AddressOf OnClockTick

            _isDarkMode = AppSettingsService.Instance.Settings.IsDarkMode
            CurrentView = LoginViewModel
            AddHandler InMemoryDataStore.Instance.SaleCompleted, Sub() UpdateStatus()
            AddHandler InMemoryDataStore.Instance.InventoryChanged, Sub() UpdateStatus()
            AddHandler InMemoryDataStore.Instance.AppointmentsChanged, Sub() UpdateStatus()
        End Sub

        Public Property LoginViewModel As LoginViewModel
        Public Property DashboardViewModel As DashboardViewModel
        Public Property CashierViewModel As CashierViewModel
        Public Property TransactionsViewModel As TransactionsViewModel
        Public Property InventoryViewModel As InventoryViewModel
        Public Property ReportsViewModel As ReportsViewModel
        Public Property MasterFilesViewModel As MasterFilesViewModel
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
                OnPropertyChanged(NameOf(DrawerColumnWidth))
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
                OnPropertyChanged(NameOf(DrawerColumnWidth))
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
                Return FormatHoursForDay(Date.Today)
            End Get
        End Property

        Public ReadOnly Property BusinessHoursHeaderText As String
            Get
                Return $"Business Hours : {FormatHoursForDay(Date.Today)}"
            End Get
        End Property

        Private Shared Function FormatHoursForDay(day As Date) As String
            Dim hours = BusinessHoursService.GetHours(day)
            Dim openLabel = Date.Today.Add(hours.Open).ToString("h:mm tt")
            Dim closeLabel = Date.Today.Add(hours.Close).ToString("h:mm tt")
            Dim dayType = If(BusinessHoursService.IsWeekend(day), "Weekend", "Weekday")
            Return $"{dayType} {openLabel} – {closeLabel}"
        End Function

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

        Public ReadOnly Property DrawerColumnWidth As GridLength
            Get
                Return If(IsLoggedIn AndAlso IsDrawerOpen,
                          New GridLength(DrawerWidth),
                          New GridLength(0))
            End Get
        End Property

        Public Property NavigateDashboardCommand As RelayCommand
        Public Property NavigateCashierCommand As RelayCommand
        Public Property NavigateTransactionsCommand As RelayCommand
        Public Property NavigateInventoryCommand As RelayCommand
        Public Property FilterLowStockCommand As RelayCommand
        Public Property NavigateReportsCommand As RelayCommand
        Public Property NavigateMasterFilesCommand As RelayCommand
        Public Property NavigateAppointmentsCommand As RelayCommand
        Public Property NavigateSettingsCommand As RelayCommand
        Public Property LogoutCommand As RelayCommand
        Public Property ToggleDrawerCommand As RelayCommand
        Public Property CloseDrawerCommand As RelayCommand

        Private Sub OnLoginSuccess()
            IsLoggedIn = True
            UpdateStatus()
            _clockTimer.Start()
            NavigateDashboard()
        End Sub

        Private Sub NavigateDashboard()
            DashboardViewModel.LoadDashboard()
            CurrentView = DashboardViewModel
            CurrentNavKey = "Dashboard"
        End Sub

        Private Sub NavigateCashier()
            CashierViewModel.RefreshLookups()
            CurrentView = CashierViewModel
            CurrentNavKey = "PointOfSale"
        End Sub

        Private Sub OpenAppointmentAtPointOfSale(appt As AppointmentItem)
            CashierViewModel.LoadFromAppointment(appt)
            NavigateCashier()
        End Sub

        Private Sub NavigateTransactions()
            TransactionsViewModel.LoadTransactions()
            CurrentView = TransactionsViewModel
            CurrentNavKey = "Transactions"
        End Sub

        Private Sub NavigateInventory()
            InventoryViewModel.ShowLowStockOnly = False
            InventoryViewModel.ActiveTab = InventoryTabs.Products
            InventoryViewModel.LoadAll()
            CurrentView = InventoryViewModel
            CurrentNavKey = "Inventory"
        End Sub

        Private Sub NavigateInventoryLowStock()
            InventoryViewModel.ApplyLowStockFilter()
            CurrentView = InventoryViewModel
            CurrentNavKey = "Inventory"
        End Sub

        Private Sub NavigateReports()
            ReportsViewModel.LoadReports()
            CurrentView = ReportsViewModel
            CurrentNavKey = "Reports"
        End Sub

        Private Sub NavigateMasterFiles()
            MasterFilesViewModel.LoadFromStore()
            CurrentView = MasterFilesViewModel
            CurrentNavKey = "MasterFiles"
        End Sub

        Private Sub OpenNewAppointment()
            AppointmentsViewModel.StartNewBooking()
            CurrentView = AppointmentsViewModel
            CurrentNavKey = "Appointments"
        End Sub

        Private Sub OpenServices()
            MasterFilesViewModel.Section = "Services"
            MasterFilesViewModel.LoadFromStore()
            CurrentView = MasterFilesViewModel
            CurrentNavKey = "MasterFiles"
        End Sub

        Private Sub NavigateAppointments()
            AppointmentsViewModel.LoadAppointments()
            CurrentView = AppointmentsViewModel
            CurrentNavKey = "Appointments"
        End Sub

        Private Sub NavigateSettings()
            SettingsViewModel.LoadFromSettings()
            CurrentView = SettingsViewModel
            CurrentNavKey = "Settings"
        End Sub

        Private Sub Logout()
            _clockTimer.Stop()
            CloseDrawer()
            SessionContext.CurrentUser = Nothing
            IsLoggedIn = False
            CurrentNavKey = String.Empty
            CurrentView = LoginViewModel
            LoginViewModel.Username = String.Empty
            LoginViewModel.Password = String.Empty
            LoginViewModel.ErrorMessage = String.Empty
            GreetingText = String.Empty
            HeaderDateTimeText = String.Empty
        End Sub

        Private Sub ToggleDrawer()
            IsDrawerOpen = Not IsDrawerOpen
        End Sub

        Private Sub CloseDrawer()
            IsDrawerOpen = False
        End Sub

        Private Sub OnClockTick()
            RefreshHeaderTexts()
            If (DateTime.Now - _lastAppointmentStatusRefresh).TotalMinutes >= 1 Then
                _lastAppointmentStatusRefresh = DateTime.Now
                Dim store = InMemoryDataStore.Instance
                If store.RefreshAppointmentStatuses() Then
                    store.PersistAppointments()
                    UpdateStatus()
                End If
            End If
        End Sub

        Private Sub UpdateStatus()
            LowStockCount = InMemoryDataStore.Instance.GetLowStockCount()
            AppointmentCountToday = InMemoryDataStore.Instance.Appointments.
                Where(Function(a) a.StartTime.Date = Date.Today AndAlso a.Status = AppointmentStatuses.Scheduled).Count()
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
            HeaderDateTimeText = now.ToString("ddd, MMMM d, yyyy | h:mm:ss tt")
            OnPropertyChanged(NameOf(BusinessHoursText))
            OnPropertyChanged(NameOf(BusinessHoursHeaderText))
        End Sub

        Private Sub NotifyNavCommands()
            NavigateDashboardCommand.NotifyCanExecuteChanged()
            NavigateCashierCommand.NotifyCanExecuteChanged()
            NavigateTransactionsCommand.NotifyCanExecuteChanged()
            NavigateInventoryCommand.NotifyCanExecuteChanged()
            FilterLowStockCommand.NotifyCanExecuteChanged()
            NavigateReportsCommand.NotifyCanExecuteChanged()
            NavigateMasterFilesCommand.NotifyCanExecuteChanged()
            NavigateAppointmentsCommand.NotifyCanExecuteChanged()
            NavigateSettingsCommand.NotifyCanExecuteChanged()
            LogoutCommand.NotifyCanExecuteChanged()
            ToggleDrawerCommand.NotifyCanExecuteChanged()
            CloseDrawerCommand.NotifyCanExecuteChanged()
        End Sub
    End Class
End Namespace
