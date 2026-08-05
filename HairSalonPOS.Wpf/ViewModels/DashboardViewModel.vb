Imports System.Collections.ObjectModel
Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class DashboardViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Private _todaySales As Decimal
        Private _transactionCount As Integer
        Private _appointmentCount As Integer
        Private _lowStockCount As Integer
        Private _dailyChart As DashboardLineChart
        Private _weeklyChart As DashboardLineChart
        Private _yearlyChart As DashboardLineChart

        Public Sub New()
            Appointments = New ObservableCollection(Of DashboardAppointmentRow)()
            RecentSales = New ObservableCollection(Of DashboardSaleRow)()
            LowStockAlerts = New ObservableCollection(Of LowStockAlertRow)()
            DailyChart = New DashboardLineChart()
            WeeklyChart = New DashboardLineChart()
            YearlyChart = New DashboardLineChart()

            AddHandler _store.SaleCompleted, Sub() LoadDashboard()
            AddHandler _store.InventoryChanged, Sub() LoadDashboard()
            AddHandler _store.AppointmentsChanged, Sub() LoadDashboard()
        End Sub

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

        Public Property AppointmentCount As Integer
            Get
                Return _appointmentCount
            End Get
            Private Set(value As Integer)
                SetProperty(_appointmentCount, value)
            End Set
        End Property

        Public Property LowStockCount As Integer
            Get
                Return _lowStockCount
            End Get
            Private Set(value As Integer)
                SetProperty(_lowStockCount, value)
                OnPropertyChanged(NameOf(HasLowStockAlerts))
            End Set
        End Property

        Public Property DailyChart As DashboardLineChart
            Get
                Return _dailyChart
            End Get
            Private Set(value As DashboardLineChart)
                SetProperty(_dailyChart, value)
            End Set
        End Property

        Public Property WeeklyChart As DashboardLineChart
            Get
                Return _weeklyChart
            End Get
            Private Set(value As DashboardLineChart)
                SetProperty(_weeklyChart, value)
            End Set
        End Property

        Public Property YearlyChart As DashboardLineChart
            Get
                Return _yearlyChart
            End Get
            Private Set(value As DashboardLineChart)
                SetProperty(_yearlyChart, value)
            End Set
        End Property

        Public ReadOnly Property HasLowStockAlerts As Boolean
            Get
                Return LowStockCount > 0
            End Get
        End Property

        Public Property Appointments As ObservableCollection(Of DashboardAppointmentRow)
        Public Property RecentSales As ObservableCollection(Of DashboardSaleRow)
        Public Property LowStockAlerts As ObservableCollection(Of LowStockAlertRow)

        Public Sub LoadDashboard()
            Dim today = Date.Today
            Dim salesToday = _store.Sales.Where(Function(s) s.SaleDate.Date = today).OrderByDescending(Function(s) s.SaleDate).ToList()

            TransactionCount = salesToday.Count
            TodaySales = If(salesToday.Count > 0, salesToday.Sum(Function(s) s.Total), 0D)

            AppointmentCount = _store.Appointments.Where(Function(a) a.StartTime.Date = today).Count()
            Appointments = New ObservableCollection(Of DashboardAppointmentRow)(
                _store.Appointments.Where(Function(a) a.StartTime.Date = today).
                OrderBy(Function(a) a.StartTime).
                Select(Function(a) New DashboardAppointmentRow With {
                    .TimeLabel = a.StartTime.ToString("h:mm tt"),
                    .CustomerName = a.CustomerName,
                    .ServiceName = a.ServiceName,
                    .StaffName = a.StaffName
                }))
            OnPropertyChanged(NameOf(Appointments))

            RecentSales = New ObservableCollection(Of DashboardSaleRow)(
                salesToday.Take(5).Select(Function(s) New DashboardSaleRow With {
                    .ReceiptNumber = s.ReceiptNumber,
                    .TimeLabel = s.SaleDate.ToString("h:mm tt"),
                    .CustomerName = s.CustomerName,
                    .Total = s.Total
                }))
            OnPropertyChanged(NameOf(RecentSales))

            Dim lowStockProducts = _store.Products.Where(Function(p) p.StockOnHand <= p.ReorderLevel).OrderBy(Function(p) p.StockOnHand).ToList()
            LowStockCount = lowStockProducts.Count
            LowStockAlerts = New ObservableCollection(Of LowStockAlertRow)(
                lowStockProducts.Select(Function(p) New LowStockAlertRow With {
                    .ProductName = p.Name,
                    .StockOnHand = p.StockOnHand,
                    .ReorderLevel = p.ReorderLevel
                }))
            OnPropertyChanged(NameOf(LowStockAlerts))

            DailyChart = SalesChartBuilder.BuildDailyChart(_store.Sales, today, "Daily analytics", "Sales by hour today")
            WeeklyChart = SalesChartBuilder.BuildWeeklyChart(_store.Sales, today, "Weekly analytics", "Sales this week")
            YearlyChart = SalesChartBuilder.BuildYearlyChart(_store.Sales, today, "Yearly analytics", $"Sales in {today.Year}")
        End Sub
    End Class
End Namespace
