Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class DashboardViewModel
        Inherits ViewModelBase

        Public Shared ReadOnly PeriodOptions As String() = {"This Day", "This Week", "This Month", "This Year"}
        Public Shared ReadOnly StaffAnalyticsPeriodOptions As String() = {"This Day", "This Week", "This Month"}
        Public ReadOnly Property FilterOptions As String()
            Get
                Return PeriodOptions
            End Get
        End Property
        Public ReadOnly Property StaffPeriodOptions As String()
            Get
                Return StaffAnalyticsPeriodOptions
            End Get
        End Property
        Private Const OverviewChartWidth As Double = 420

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _history As TransactionHistoryService = TransactionHistoryService.Instance

        Private _todaySales As Decimal
        Private _transactionCount As Integer
        Private _lowStockCount As Integer
        Private _appointmentCount As Integer
        Private _salesChangeText As String = "vs yesterday  0.0%"
        Private _txChangeText As String = "vs yesterday  0.0%"
        Private _salesChangeUp As Boolean
        Private _txChangeUp As Boolean
        Private _overviewChart As DashboardLineChart
        Private _salesPeriod As String = "This Week"
        Private _topServicesPeriod As String = "This Month"
        Private _overviewTotal As Decimal
        Private _overviewAverage As Decimal
        Private _overviewBestLabel As String = "—"
        Private _overviewChangeText As String = "vs prior  0.0%"
        Private _overviewChangeUp As Boolean
        Private _staffAnalyticsPeriod As String = "This Week"
        Private _cashRevenue As Decimal
        Private _cashTransactionCount As Integer
        Private _gcashRevenue As Decimal
        Private _gcashTransactionCount As Integer
        Private _paymentTotalRevenue As Decimal
        Private _paymentTotalTransactionCount As Integer
        Private _cashPercentLabel As String = "0%"
        Private _gcashPercentLabel As String = "0%"
        Private _allSales As List(Of SaleRecord) = New List(Of SaleRecord)()

        Public Sub New()
            Appointments = New ObservableCollection(Of DashboardAppointmentRow)()
            RecentSales = New ObservableCollection(Of DashboardSaleRow)()
            LowStockAlerts = New ObservableCollection(Of LowStockAlertRow)()
            TopServices = New ObservableCollection(Of DashboardTopServiceRow)()
            CategorySlices = New ObservableCollection(Of DashboardDonutSlice)()
            PaymentMethodSlices = New ObservableCollection(Of DashboardDonutSlice)()
            StaffPerformanceRows = New ObservableCollection(Of DashboardStaffPerformanceRow)()
            OverviewChart = New DashboardLineChart()

            NewSaleCommand = New RelayCommand(Sub() _goToPos?.Invoke())
            NewAppointmentCommand = New RelayCommand(Sub() _goToNewAppointment?.Invoke())
            GoToInventoryCommand = New RelayCommand(Sub() _goToInventory?.Invoke(), Function() CanViewAdminScreens)
            ReorderProductCommand = New RelayCommand(Of String)(Sub(sku) _goToStockIn?.Invoke(sku), Function(sku) CanViewAdminScreens AndAlso Not String.IsNullOrWhiteSpace(sku))
            GoToReportsCommand = New RelayCommand(Sub() _goToReports?.Invoke())
            GoToAppointmentsCommand = New RelayCommand(Sub() _goToAppointments?.Invoke())
            GoToTransactionsCommand = New RelayCommand(Sub() _goToTransactions?.Invoke())
            GoToServicesCommand = New RelayCommand(Sub() _goToServices?.Invoke(), Function() CanViewAdminScreens)

            AddHandler _store.SaleCompleted, Sub() LoadDashboard()
            AddHandler _store.InventoryChanged, Sub() LoadDashboard()
            AddHandler _store.AppointmentsChanged, Sub() LoadDashboard()
        End Sub

        Private _goToPos As Action
        Private _goToNewAppointment As Action
        Private _goToAppointments As Action
        Private _goToTransactions As Action
        Private _goToInventory As Action
        Private _goToStockIn As Action(Of String)
        Private _goToReports As Action
        Private _goToServices As Action

        Public Sub BindNavigation(goToPos As Action,
                                  goToNewAppointment As Action,
                                  goToAppointments As Action,
                                  goToTransactions As Action,
                                  goToInventory As Action,
                                  goToStockIn As Action(Of String),
                                  goToReports As Action,
                                  goToServices As Action)
            _goToPos = goToPos
            _goToNewAppointment = goToNewAppointment
            _goToAppointments = goToAppointments
            _goToTransactions = goToTransactions
            _goToInventory = goToInventory
            _goToStockIn = goToStockIn
            _goToReports = goToReports
            _goToServices = goToServices
        End Sub

        Public ReadOnly Property CanViewAdminScreens As Boolean
            Get
                Return SessionContext.IsAdmin
            End Get
        End Property

        Public ReadOnly Property AppointmentDateLabel As String
            Get
                Return Date.Today.ToString("MMMM d, yyyy")
            End Get
        End Property

        Public Property TodaySales As Decimal
            Get
                Return _todaySales
            End Get
            Private Set(value As Decimal)
                SetProperty(_todaySales, value)
            End Set
        End Property

        Public Property TransactionCount As Integer
            Get
                Return _transactionCount
            End Get
            Private Set(value As Integer)
                SetProperty(_transactionCount, value)
            End Set
        End Property

        Public Property LowStockCount As Integer
            Get
                Return _lowStockCount
            End Get
            Private Set(value As Integer)
                SetProperty(_lowStockCount, value)
                OnPropertyChanged(NameOf(LowStockSubtitle))
            End Set
        End Property

        Public ReadOnly Property LowStockSubtitle As String
            Get
                If LowStockCount <= 0 Then Return "All stock levels OK"
                If LowStockCount = 1 Then Return "1 item needs restock"
                Return $"{LowStockCount} items need restock"
            End Get
        End Property

        Public Property AppointmentCount As Integer
            Get
                Return _appointmentCount
            End Get
            Private Set(value As Integer)
                SetProperty(_appointmentCount, value)
            End Set
        End Property

        Public Property SalesChangeText As String
            Get
                Return _salesChangeText
            End Get
            Private Set(value As String)
                SetProperty(_salesChangeText, value)
            End Set
        End Property

        Public Property TxChangeText As String
            Get
                Return _txChangeText
            End Get
            Private Set(value As String)
                SetProperty(_txChangeText, value)
            End Set
        End Property

        Public Property SalesChangeUp As Boolean
            Get
                Return _salesChangeUp
            End Get
            Private Set(value As Boolean)
                SetProperty(_salesChangeUp, value)
            End Set
        End Property

        Public Property TxChangeUp As Boolean
            Get
                Return _txChangeUp
            End Get
            Private Set(value As Boolean)
                SetProperty(_txChangeUp, value)
            End Set
        End Property

        Public Property SalesPeriod As String
            Get
                Return _salesPeriod
            End Get
            Set(value As String)
                If SetProperty(_salesPeriod, If(value, "This Week")) Then
                    RefreshPeriodVisuals()
                End If
            End Set
        End Property

        Public Property TopServicesPeriod As String
            Get
                Return _topServicesPeriod
            End Get
            Set(value As String)
                If SetProperty(_topServicesPeriod, If(value, "This Month")) Then
                    RefreshTopServices()
                End If
            End Set
        End Property

        Public Property StaffAnalyticsPeriod As String
            Get
                Return _staffAnalyticsPeriod
            End Get
            Set(value As String)
                If SetProperty(_staffAnalyticsPeriod, If(value, "This Week")) Then
                    RefreshStaffAnalytics()
                End If
            End Set
        End Property

        Public Property OverviewChart As DashboardLineChart
            Get
                Return _overviewChart
            End Get
            Private Set(value As DashboardLineChart)
                SetProperty(_overviewChart, value)
            End Set
        End Property

        Public Property OverviewTotal As Decimal
            Get
                Return _overviewTotal
            End Get
            Private Set(value As Decimal)
                SetProperty(_overviewTotal, value)
            End Set
        End Property

        Public Property OverviewAverage As Decimal
            Get
                Return _overviewAverage
            End Get
            Private Set(value As Decimal)
                SetProperty(_overviewAverage, value)
            End Set
        End Property

        Public Property OverviewBestLabel As String
            Get
                Return _overviewBestLabel
            End Get
            Private Set(value As String)
                SetProperty(_overviewBestLabel, value)
            End Set
        End Property

        Public Property OverviewChangeText As String
            Get
                Return _overviewChangeText
            End Get
            Private Set(value As String)
                SetProperty(_overviewChangeText, value)
            End Set
        End Property

        Public Property OverviewChangeUp As Boolean
            Get
                Return _overviewChangeUp
            End Get
            Private Set(value As Boolean)
                SetProperty(_overviewChangeUp, value)
            End Set
        End Property

        Public ReadOnly Property HasUpcomingAppointments As Boolean
            Get
                Return Appointments IsNot Nothing AndAlso Appointments.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasTopServices As Boolean
            Get
                Return TopServices IsNot Nothing AndAlso TopServices.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasCategorySlices As Boolean
            Get
                Return CategorySlices IsNot Nothing AndAlso CategorySlices.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasInventoryAlerts As Boolean
            Get
                Return LowStockAlerts IsNot Nothing AndAlso LowStockAlerts.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasRecentSales As Boolean
            Get
                Return RecentSales IsNot Nothing AndAlso RecentSales.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasPaymentMethodData As Boolean
            Get
                Return PaymentTotalRevenue > 0D OrElse PaymentTotalTransactionCount > 0
            End Get
        End Property

        Public Property CashRevenue As Decimal
            Get
                Return _cashRevenue
            End Get
            Private Set(value As Decimal)
                SetProperty(_cashRevenue, value)
            End Set
        End Property

        Public Property CashTransactionCount As Integer
            Get
                Return _cashTransactionCount
            End Get
            Private Set(value As Integer)
                SetProperty(_cashTransactionCount, value)
            End Set
        End Property

        Public Property GcashRevenue As Decimal
            Get
                Return _gcashRevenue
            End Get
            Private Set(value As Decimal)
                SetProperty(_gcashRevenue, value)
            End Set
        End Property

        Public Property GcashTransactionCount As Integer
            Get
                Return _gcashTransactionCount
            End Get
            Private Set(value As Integer)
                SetProperty(_gcashTransactionCount, value)
            End Set
        End Property

        Public Property PaymentTotalRevenue As Decimal
            Get
                Return _paymentTotalRevenue
            End Get
            Private Set(value As Decimal)
                SetProperty(_paymentTotalRevenue, value)
                OnPropertyChanged(NameOf(HasPaymentMethodData))
            End Set
        End Property

        Public Property PaymentTotalTransactionCount As Integer
            Get
                Return _paymentTotalTransactionCount
            End Get
            Private Set(value As Integer)
                SetProperty(_paymentTotalTransactionCount, value)
                OnPropertyChanged(NameOf(HasPaymentMethodData))
            End Set
        End Property

        Public Property CashPercentLabel As String
            Get
                Return _cashPercentLabel
            End Get
            Private Set(value As String)
                SetProperty(_cashPercentLabel, value)
            End Set
        End Property

        Public Property GcashPercentLabel As String
            Get
                Return _gcashPercentLabel
            End Get
            Private Set(value As String)
                SetProperty(_gcashPercentLabel, value)
            End Set
        End Property

        Public Property Appointments As ObservableCollection(Of DashboardAppointmentRow)
        Public Property RecentSales As ObservableCollection(Of DashboardSaleRow)
        Public Property LowStockAlerts As ObservableCollection(Of LowStockAlertRow)
        Public Property TopServices As ObservableCollection(Of DashboardTopServiceRow)
        Public Property CategorySlices As ObservableCollection(Of DashboardDonutSlice)
        Public Property PaymentMethodSlices As ObservableCollection(Of DashboardDonutSlice)
        Public Property StaffPerformanceRows As ObservableCollection(Of DashboardStaffPerformanceRow)

        Public Property NewSaleCommand As RelayCommand
        Public Property NewAppointmentCommand As RelayCommand
        Public Property GoToInventoryCommand As RelayCommand
        Public Property ReorderProductCommand As RelayCommand(Of String)
        Public Property GoToReportsCommand As RelayCommand
        Public Property GoToAppointmentsCommand As RelayCommand
        Public Property GoToTransactionsCommand As RelayCommand
        Public Property GoToServicesCommand As RelayCommand

        Public Sub LoadDashboard()
            If _store.RefreshAppointmentStatuses() Then
                _store.PersistAppointments()
            End If

            Dim today = Date.Today
            Dim fromDate = New Date(today.Year - 1, 1, 1)
            _allSales = _history.GetSales(fromDate, today.AddDays(1))

            Dim salesToday = InRange(_allSales, today, today.AddDays(1))
            Dim salesYesterday = InRange(_allSales, today.AddDays(-1), today)

            TodaySales = salesToday.Sum(Function(s) s.Total)
            TransactionCount = salesToday.Count
            SetChange(TodaySales, salesYesterday.Sum(Function(s) s.Total), "vs yesterday", Sub(t, u)
                                                                                                SalesChangeText = t
                                                                                                SalesChangeUp = u
                                                                                            End Sub)

            Dim txYesterday = salesYesterday.Count
            SetChange(TransactionCount, txYesterday, "vs yesterday", Sub(t, u)
                                                                         TxChangeText = t
                                                                         TxChangeUp = u
                                                                     End Sub)

            _store.RefreshAppointmentStatuses()
            AppointmentCount = _store.Appointments.Where(
                Function(a) a.StartTime.Date = today AndAlso
                              (a.Status = AppointmentStatuses.Scheduled OrElse a.Status = AppointmentStatuses.Confirmed)).Count()
            OnPropertyChanged(NameOf(AppointmentDateLabel))

            Dim upcoming = _store.Appointments.
                Where(Function(a) (a.Status = AppointmentStatuses.Scheduled OrElse a.Status = AppointmentStatuses.Confirmed) AndAlso
                                  a.StartTime >= DateTime.Now).
                OrderBy(Function(a) a.StartTime).
                Take(5).
                ToList()
            If upcoming.Count = 0 Then
                upcoming = _store.Appointments.
                    Where(Function(a) a.StartTime.Date = today AndAlso
                                      (a.Status = AppointmentStatuses.Scheduled OrElse a.Status = AppointmentStatuses.Confirmed)).
                    OrderBy(Function(a) a.StartTime).
                    Take(5).
                    ToList()
            End If
            Appointments = New ObservableCollection(Of DashboardAppointmentRow)(
                upcoming.Select(Function(a) New DashboardAppointmentRow With {
                    .TimeLabel = a.StartTime.ToString("h:mm tt"),
                    .CustomerName = a.CustomerName,
                    .ServiceName = a.ServiceName,
                    .StaffName = a.StaffName,
                    .StatusLabel = a.StatusLabel,
                    .IsConfirmed = a.Status = AppointmentStatuses.Confirmed
                }))
            OnPropertyChanged(NameOf(Appointments))
            OnPropertyChanged(NameOf(HasUpcomingAppointments))

            RecentSales = New ObservableCollection(Of DashboardSaleRow)(
                salesToday.OrderByDescending(Function(s) s.SaleDate).Take(5).Select(Function(s) New DashboardSaleRow With {
                    .ReceiptNumber = s.ReceiptNumber,
                    .TimeLabel = s.SaleDate.ToString("h:mm tt"),
                    .CustomerName = If(String.IsNullOrWhiteSpace(s.CustomerName), "Walk-in", s.CustomerName),
                    .Total = s.Total
                }))
            OnPropertyChanged(NameOf(RecentSales))
            OnPropertyChanged(NameOf(HasRecentSales))

            Dim lowStockProducts = _store.Products.Where(Function(p) p.StockOnHand <= p.ReorderLevel).OrderBy(Function(p) p.StockOnHand).ToList()
            LowStockCount = lowStockProducts.Count
            LowStockAlerts = New ObservableCollection(Of LowStockAlertRow)(
                lowStockProducts.Select(Function(p) New LowStockAlertRow With {
                    .Sku = p.Sku,
                    .ProductName = p.Name,
                    .StockOnHand = p.StockOnHand,
                    .ReorderLevel = p.ReorderLevel,
                    .ImagePath = p.ImagePath
                }))
            OnPropertyChanged(NameOf(LowStockAlerts))
            OnPropertyChanged(NameOf(HasInventoryAlerts))

            RefreshPeriodVisuals()
            RefreshTopServices()
            RefreshStaffAnalytics()
            OnPropertyChanged(NameOf(CanViewAdminScreens))
            GoToInventoryCommand.NotifyCanExecuteChanged()
            ReorderProductCommand.NotifyCanExecuteChanged()
            GoToServicesCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub RefreshPeriodVisuals()
            Dim today = Date.Today
            Dim range = PeriodRange(SalesPeriod, today)
            Dim currentSales = InRange(_allSales, range.FromDate, range.ToDateExclusive)
            Dim prior = PeriodRange(SalesPeriod, today, previous:=True)
            Dim priorSales = InRange(_allSales, prior.FromDate, prior.ToDateExclusive)

            Select Case SalesPeriod
                Case "This Day"
                    OverviewChart = SalesChartBuilder.BuildDailyChart(currentSales, today, String.Empty, String.Empty, OverviewChartWidth)
                Case "This Month"
                    OverviewChart = SalesChartBuilder.BuildMonthlyChart(currentSales, today, String.Empty, String.Empty, OverviewChartWidth, asArea:=True)
                Case "This Year"
                    OverviewChart = SalesChartBuilder.BuildYearlyChart(currentSales, today, String.Empty, String.Empty, OverviewChartWidth, asArea:=True)
                Case Else
                    OverviewChart = SalesChartBuilder.BuildWeeklyChart(currentSales, today, String.Empty, String.Empty, OverviewChartWidth, asArea:=True)
            End Select

            Dim points = If(OverviewChart.Points, New ObservableCollection(Of DashboardChartPoint)())
            OverviewTotal = currentSales.Sum(Function(s) s.Total)
            OverviewAverage = If(points.Count > 0, Math.Round(OverviewTotal / points.Count, 2), 0D)
            Dim best = points.OrderByDescending(Function(p) p.Amount).FirstOrDefault()
            OverviewBestLabel = If(best Is Nothing OrElse best.Amount <= 0D, "—", best.Label)

            Dim priorTotal = priorSales.Sum(Function(s) s.Total)
            Dim changeLabel = SalesPeriod.Replace("This ", "prior ").ToLowerInvariant()
            SetChange(OverviewTotal, priorTotal, $"vs {changeLabel}", Sub(t, u)
                                                                          OverviewChangeText = t
                                                                          OverviewChangeUp = u
                                                                      End Sub)

            Dim grouped = currentSales.
                SelectMany(Function(s) SafeLines(s)).
                GroupBy(Function(l) ResolveCategory(l)).
                Select(Function(g) Tuple.Create(g.Key, g.Sum(Function(x) x.LineTotal))).
                Where(Function(t) t.Item2 > 0D).
                OrderByDescending(Function(t) t.Item2).
                ToList()
            CategorySlices = New ObservableCollection(Of DashboardDonutSlice)(DonutChartBuilder.Build(grouped))
            OnPropertyChanged(NameOf(CategorySlices))
            OnPropertyChanged(NameOf(HasCategorySlices))
        End Sub

        Private Sub RefreshTopServices()
            Dim range = PeriodRange(TopServicesPeriod, Date.Today)
            Dim sales = InRange(_allSales, range.FromDate, range.ToDateExclusive)
            Dim serviceTotals = sales.
                SelectMany(Function(s) SafeLines(s).Where(Function(l) l.IsService)).
                GroupBy(Function(l) l.Name).
                Select(Function(g) New With {.Name = g.Key, .Amount = g.Sum(Function(x) x.LineTotal)}).
                OrderByDescending(Function(x) x.Amount).
                Take(5).
                ToList()

            Dim maxAmount = If(serviceTotals.Count > 0, serviceTotals(0).Amount, 1D)
            TopServices = New ObservableCollection(Of DashboardTopServiceRow)(
                serviceTotals.Select(Function(s) New DashboardTopServiceRow With {
                    .Name = s.Name,
                    .Amount = s.Amount,
                    .BarWidth = If(maxAmount > 0, CDbl(s.Amount / maxAmount * 140), 0)
                }))
            OnPropertyChanged(NameOf(TopServices))
            OnPropertyChanged(NameOf(HasTopServices))
        End Sub

        Private Sub RefreshStaffAnalytics()
            Dim range = PeriodRange(StaffAnalyticsPeriod, Date.Today)
            Dim sales = InRange(_allSales, range.FromDate, range.ToDateExclusive)
            Dim activeStaff = _store.Staff.Where(Function(s) s.IsActive).ToList()

            Dim cashSales = sales.Where(Function(s) String.Equals(s.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)).ToList()
            Dim gcashSales = sales.Where(Function(s) String.Equals(s.PaymentMethod, "GCash", StringComparison.OrdinalIgnoreCase)).ToList()

            CashRevenue = cashSales.Sum(Function(s) s.Total)
            CashTransactionCount = cashSales.Count
            GcashRevenue = gcashSales.Sum(Function(s) s.Total)
            GcashTransactionCount = gcashSales.Count
            PaymentTotalRevenue = CashRevenue + GcashRevenue
            PaymentTotalTransactionCount = CashTransactionCount + GcashTransactionCount
            CashPercentLabel = If(PaymentTotalRevenue > 0D, $"{CashRevenue / PaymentTotalRevenue:P0}", "0%")
            GcashPercentLabel = If(PaymentTotalRevenue > 0D, $"{GcashRevenue / PaymentTotalRevenue:P0}", "0%")

            PaymentMethodSlices = New ObservableCollection(Of DashboardDonutSlice)(
                DonutChartBuilder.Build(New List(Of Tuple(Of String, Decimal)) From {
                    Tuple.Create("Cash", CashRevenue),
                    Tuple.Create("GCash", GcashRevenue)
                }))
            OnPropertyChanged(NameOf(PaymentMethodSlices))
            OnPropertyChanged(NameOf(HasPaymentMethodData))

            Dim performanceRows = activeStaff.Select(Function(staff)
                                                         Dim staffSales = sales.Where(Function(s) Not String.IsNullOrWhiteSpace(s.StylistName) AndAlso
                                                                                          s.StylistName.Equals(staff.Name, StringComparison.OrdinalIgnoreCase))
                                                         Dim serviceCount = staffSales.
                                                             SelectMany(Function(s) SafeLines(s).Where(Function(l) l.IsService)).
                                                             Sum(Function(l) l.Quantity)
                                                         Return New DashboardStaffPerformanceRow With {
                                                             .StaffName = staff.Name,
                                                             .ImagePath = staff.ImagePath,
                                                             .ServicesCompleted = serviceCount
                                                         }
                                                     End Function).
                OrderByDescending(Function(r) r.ServicesCompleted).
                ThenBy(Function(r) r.StaffName).
                Select(Function(r, index)
                           r.Rank = index + 1
                           Return r
                       End Function).
                ToList()

            StaffPerformanceRows = New ObservableCollection(Of DashboardStaffPerformanceRow)(performanceRows)
            OnPropertyChanged(NameOf(StaffPerformanceRows))
        End Sub

        Private Shared Function InRange(sales As IEnumerable(Of SaleRecord), fromDate As Date, toDateExclusive As Date) As List(Of SaleRecord)
            Return sales.Where(Function(s) s.SaleDate >= fromDate AndAlso s.SaleDate < toDateExclusive).ToList()
        End Function

        Private Shared Function PeriodRange(period As String, anchor As Date, Optional previous As Boolean = False) As (FromDate As Date, ToDateExclusive As Date)
            Select Case period
                Case "This Day"
                    Dim day = If(previous, anchor.AddDays(-1), anchor)
                    Return (day, day.AddDays(1))
                Case "This Month"
                    Dim start = New Date(anchor.Year, anchor.Month, 1)
                    If previous Then start = start.AddMonths(-1)
                    Return (start, start.AddMonths(1))
                Case "This Year"
                    Dim start = New Date(anchor.Year, 1, 1)
                    If previous Then start = start.AddYears(-1)
                    Return (start, start.AddYears(1))
                Case Else
                    Dim start = anchor.AddDays(-CInt(anchor.DayOfWeek))
                    If previous Then start = start.AddDays(-7)
                    Return (start, start.AddDays(7))
            End Select
        End Function

        Private Shared Sub SetChange(current As Decimal, previous As Decimal, prefix As String, apply As Action(Of String, Boolean))
            Dim up = current >= previous
            Dim text As String
            If previous = 0D Then
                text = If(current = 0D, $"{prefix}  0.0%", $"{prefix}  +100.0%")
                up = current > 0D
            Else
                Dim pct = (current - previous) / previous * 100D
                text = $"{prefix}  {pct:+0.0;-0.0;0.0}%"
            End If
            apply(text, up)
        End Sub

        Private Shared Function SafeLines(sale As SaleRecord) As IEnumerable(Of SaleLineRecord)
            If sale.Lines Is Nothing Then Return Enumerable.Empty(Of SaleLineRecord)()
            Return sale.Lines
        End Function

        Private Function ResolveCategory(line As SaleLineRecord) As String
            If line.IsService Then
                Dim svc = _store.Services.FirstOrDefault(Function(s) s.Name.Equals(line.Name, StringComparison.OrdinalIgnoreCase))
                If svc IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(svc.Category) Then Return svc.Category
                Return "Services"
            End If

            Dim prod = _store.Products.FirstOrDefault(Function(p) p.Name.Equals(line.Name, StringComparison.OrdinalIgnoreCase))
            If prod IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(prod.Category) Then Return prod.Category
            Return "Products"
        End Function
    End Class
End Namespace
