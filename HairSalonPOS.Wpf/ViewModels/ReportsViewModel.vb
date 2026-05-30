Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports Microsoft.Win32

Namespace ViewModels
    Public Class ReportsViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Private _period As String = "Daily"
        Private _selectedDate As Date = Date.Today
        Private _transactionCount As Integer
        Private _totalSales As Decimal
        Private _averageSale As Decimal
        Private _topService As String = "—"
        Private _statusMessage As String = String.Empty

        Public Sub New()
            Sales = New ObservableCollection(Of SaleRecord)()
            RevenueBars = New ObservableCollection(Of RevenueBarItem)()
            StylistPerformance = New ObservableCollection(Of StylistPerformanceItem)()

            RefreshCommand = New RelayCommand(AddressOf LoadReports)
            SetDailyCommand = New RelayCommand(Sub() Period = "Daily")
            SetWeeklyCommand = New RelayCommand(Sub() Period = "Weekly")
            SetMonthlyCommand = New RelayCommand(Sub() Period = "Monthly")
            ExportPdfCommand = New RelayCommand(AddressOf ExportPdf)
            ExportExcelCommand = New RelayCommand(AddressOf ExportExcel)

            AddHandler _store.SaleCompleted, Sub() LoadReports()
            LoadReports()
        End Sub

        Public Property Period As String
            Get
                Return _period
            End Get
            Set(value As String)
                SetProperty(_period, value)
                LoadReports()
                OnPropertyChanged(NameOf(IsDaily))
                OnPropertyChanged(NameOf(IsWeekly))
                OnPropertyChanged(NameOf(IsMonthly))
            End Set
        End Property

        Public Property SelectedDate As Date
            Get
                Return _selectedDate
            End Get
            Set(value As Date)
                SetProperty(_selectedDate, value)
                LoadReports()
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

        Public Property TotalSales As Decimal
            Get
                Return _totalSales
            End Get
            Private Set(value As Decimal)
                SetProperty(_totalSales, value)
            End Set
        End Property

        Public Property AverageSale As Decimal
            Get
                Return _averageSale
            End Get
            Private Set(value As Decimal)
                SetProperty(_averageSale, value)
            End Set
        End Property

        Public Property TopService As String
            Get
                Return _topService
            End Get
            Private Set(value As String)
                SetProperty(_topService, value)
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

        Public Property Sales As ObservableCollection(Of SaleRecord)
        Public Property RevenueBars As ObservableCollection(Of RevenueBarItem)
        Public Property StylistPerformance As ObservableCollection(Of StylistPerformanceItem)

        Public ReadOnly Property IsDaily As Boolean
            Get
                Return Period = "Daily"
            End Get
        End Property

        Public ReadOnly Property IsWeekly As Boolean
            Get
                Return Period = "Weekly"
            End Get
        End Property

        Public ReadOnly Property IsMonthly As Boolean
            Get
                Return Period = "Monthly"
            End Get
        End Property

        Public Property RefreshCommand As RelayCommand
        Public Property SetDailyCommand As RelayCommand
        Public Property SetWeeklyCommand As RelayCommand
        Public Property SetMonthlyCommand As RelayCommand
        Public Property ExportPdfCommand As RelayCommand
        Public Property ExportExcelCommand As RelayCommand

        Public Sub LoadReports()
            Dim filtered = FilterSales().OrderByDescending(Function(s) s.SaleDate).ToList()
            Sales = New ObservableCollection(Of SaleRecord)(filtered)
            TransactionCount = filtered.Count
            TotalSales = If(filtered.Count > 0, filtered.Sum(Function(s) s.Total), 0D)
            AverageSale = If(filtered.Count > 0, Math.Round(TotalSales / filtered.Count, 2), 0D)

            Dim serviceTotals = filtered.SelectMany(Function(s) s.Lines.Where(Function(l) l.IsService)).
                GroupBy(Function(l) l.Name).
                Select(Function(g) New With {.Name = g.Key, .Amount = g.Sum(Function(x) x.LineTotal)}).
                OrderByDescending(Function(x) x.Amount).ToList()

            TopService = If(serviceTotals.Count > 0, serviceTotals(0).Name, "—")

            Dim maxAmount = If(serviceTotals.Count > 0, serviceTotals(0).Amount, 1D)
            RevenueBars = New ObservableCollection(Of RevenueBarItem)(
                serviceTotals.Take(5).Select(Function(s) New RevenueBarItem With {
                    .Label = s.Name,
                    .Amount = s.Amount,
                    .BarHeight = If(maxAmount > 0, CDbl(s.Amount / maxAmount * 140), 0)
                }))
            OnPropertyChanged(NameOf(RevenueBars))

            StylistPerformance = New ObservableCollection(Of StylistPerformanceItem)(
                filtered.Where(Function(s) Not String.IsNullOrWhiteSpace(s.StylistName)).
                GroupBy(Function(s) s.StylistName).
                Select(Function(g) New StylistPerformanceItem With {
                    .StylistName = g.Key,
                    .ServiceCount = g.SelectMany(Function(s) s.Lines.Where(Function(l) l.IsService)).Sum(Function(l) l.Quantity),
                    .Revenue = g.Sum(Function(s) s.Total)
                }).OrderByDescending(Function(x) x.Revenue))

            OnPropertyChanged(NameOf(Sales))
            OnPropertyChanged(NameOf(StylistPerformance))
        End Sub

        Private Function FilterSales() As IEnumerable(Of SaleRecord)
            Select Case Period
                Case "Weekly"
                    Dim start = SelectedDate.Date.AddDays(-CInt(SelectedDate.DayOfWeek))
                    Dim endDate = start.AddDays(7)
                    Return _store.Sales.Where(Function(s) s.SaleDate.Date >= start AndAlso s.SaleDate.Date < endDate)
                Case "Monthly"
                    Return _store.Sales.Where(Function(s) s.SaleDate.Year = SelectedDate.Year AndAlso s.SaleDate.Month = SelectedDate.Month)
                Case Else
                    Return _store.Sales.Where(Function(s) s.SaleDate.Date = SelectedDate.Date)
            End Select
        End Function

        Private Sub ExportPdf()
            Dim summary = $"Cindy Hair Salon Report ({Period}){Environment.NewLine}Transactions: {TransactionCount}{Environment.NewLine}Total: {TotalSales:N2}"
            ExportService.ExportSalesPdf(Sales, summary)
            StatusMessage = "PDF report exported."
        End Sub

        Private Sub ExportExcel()
            Dim dlg As New SaveFileDialog With {.Filter = "CSV files|*.csv", .FileName = "sales_report.csv"}
            If dlg.ShowDialog() <> True Then Return
            ExportService.ExportSalesCsv(Sales, dlg.FileName)
            StatusMessage = "Excel/CSV report exported."
        End Sub
    End Class
End Namespace
