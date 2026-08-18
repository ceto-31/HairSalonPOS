Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services
Imports HairSalonPOS.Wpf.Views

Namespace ViewModels
    Public Class TransactionsViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _history As TransactionHistoryService = TransactionHistoryService.Instance
        Private ReadOnly _print As New ReceiptPrintService()

        Private _period As String = "All"
        Private _selectedDate As Date = Date.Today
        Private _searchText As String = String.Empty
        Private _transactionCount As Integer
        Private _totalSales As Decimal
        Private _averageSale As Decimal
        Private _statusMessage As String = String.Empty
        Private _allSales As List(Of SaleRecord) = New List(Of SaleRecord)()

        Public Sub New()
            Sales = New ObservableCollection(Of SaleRecord)()

            RefreshCommand = New RelayCommand(AddressOf LoadTransactions)
            SetAllCommand = New RelayCommand(Sub() Period = "All")
            SetDailyCommand = New RelayCommand(Sub() Period = "Daily")
            SetWeeklyCommand = New RelayCommand(Sub() Period = "Weekly")
            SetMonthlyCommand = New RelayCommand(Sub() Period = "Monthly")
            SetYearlyCommand = New RelayCommand(Sub() Period = "Yearly")
            PreviewReceiptCommand = New RelayCommand(Of SaleRecord)(AddressOf PreviewReceipt)
            ReprintReceiptCommand = New RelayCommand(Of SaleRecord)(AddressOf ReprintReceipt)

            AddHandler _store.SaleCompleted, Sub() LoadTransactions()
            LoadTransactions()
        End Sub

        Public Property Period As String
            Get
                Return _period
            End Get
            Set(value As String)
                SetProperty(_period, value)
                LoadTransactions()
                OnPropertyChanged(NameOf(IsAll))
                OnPropertyChanged(NameOf(IsDaily))
                OnPropertyChanged(NameOf(IsWeekly))
                OnPropertyChanged(NameOf(IsMonthly))
                OnPropertyChanged(NameOf(IsYearly))
            End Set
        End Property

        Public Property SelectedDate As Date
            Get
                Return _selectedDate
            End Get
            Set(value As Date)
                SetProperty(_selectedDate, value)
                LoadTransactions()
            End Set
        End Property

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then ApplySearchFilter()
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

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property Sales As ObservableCollection(Of SaleRecord)

        Public ReadOnly Property IsAll As Boolean
            Get
                Return Period = "All"
            End Get
        End Property

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

        Public ReadOnly Property IsYearly As Boolean
            Get
                Return Period = "Yearly"
            End Get
        End Property

        Public Property RefreshCommand As RelayCommand
        Public Property SetAllCommand As RelayCommand
        Public Property SetDailyCommand As RelayCommand
        Public Property SetWeeklyCommand As RelayCommand
        Public Property SetMonthlyCommand As RelayCommand
        Public Property SetYearlyCommand As RelayCommand
        Public Property PreviewReceiptCommand As RelayCommand(Of SaleRecord)
        Public Property ReprintReceiptCommand As RelayCommand(Of SaleRecord)

        Public Sub LoadTransactions()
            Dim range = GetDateRange()
            _allSales = _history.GetSales(range.FromDate, range.ToDateExclusive)
            ApplySearchFilter()
        End Sub

        Public Sub PreviewReceipt(sale As SaleRecord)
            If sale Is Nothing Then Return
            Dim receipt = _history.ResolveReceiptModel(sale)
            If receipt Is Nothing Then
                StatusMessage = "Could not load receipt details for this transaction."
                Return
            End If

            Dim preview As New ReceiptPreviewWindow(receipt)
            preview.Owner = System.Windows.Application.Current?.MainWindow
            preview.ShowDialog()
        End Sub

        Private Sub ReprintReceipt(sale As SaleRecord)
            If sale Is Nothing Then Return
            Dim receipt = _history.ResolveReceiptModel(sale)
            If receipt Is Nothing Then
                StatusMessage = "Could not load receipt details for reprint."
                Return
            End If

            Try
                _print.PrintReceipt(receipt, showDialog:=True)
                StatusMessage = $"Reprinted {receipt.ReceiptNumber}."
            Catch ex As Exception
                StatusMessage = $"Reprint failed: {ex.Message}"
            End Try
        End Sub

        Private Sub ApplySearchFilter()
            Dim query = _allSales.AsEnumerable()

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim()
                query = query.Where(Function(s) s.ReceiptNumber.Contains(term, StringComparison.OrdinalIgnoreCase) OrElse
                                               s.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) OrElse
                                               s.CashierName.Contains(term, StringComparison.OrdinalIgnoreCase))
            End If

            Dim filtered = query.
                OrderByDescending(Function(s) s.SaleDate).
                ThenByDescending(Function(s) ParseReceiptSequence(s.ReceiptNumber)).
                ThenByDescending(Function(s) s.SaleId).
                ToList()
            Sales = New ObservableCollection(Of SaleRecord)(filtered)
            TransactionCount = filtered.Count
            TotalSales = If(filtered.Count > 0, filtered.Sum(Function(s) s.Total), 0D)
            AverageSale = If(filtered.Count > 0, Math.Round(TotalSales / filtered.Count, 2), 0D)
            OnPropertyChanged(NameOf(Sales))
        End Sub

        Private Shared Function ParseReceiptSequence(receiptNumber As String) As Integer
            If String.IsNullOrWhiteSpace(receiptNumber) Then Return 0
            Dim digits = New String(receiptNumber.Where(Function(c) Char.IsDigit(c)).ToArray())
            Dim value As Integer
            If Integer.TryParse(digits, value) Then Return value
            Return 0
        End Function

        Private Function GetDateRange() As (FromDate As Date, ToDateExclusive As Date)
            Select Case Period
                Case "All"
                    Return (Date.MinValue, Date.Today.AddDays(1))
                Case "Weekly"
                    Dim start = SelectedDate.Date.AddDays(-CInt(SelectedDate.DayOfWeek))
                    Return (start, start.AddDays(7))
                Case "Monthly"
                    Dim start = New Date(SelectedDate.Year, SelectedDate.Month, 1)
                    Return (start, start.AddMonths(1))
                Case "Yearly"
                    Dim start = New Date(SelectedDate.Year, 1, 1)
                    Return (start, start.AddYears(1))
                Case Else
                    Return (SelectedDate.Date, SelectedDate.Date.AddDays(1))
            End Select
        End Function
    End Class
End Namespace
