Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class TransactionHistoryService
        Private Shared ReadOnly _instance As New Lazy(Of TransactionHistoryService)(Function() New TransactionHistoryService())
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _receipts As ReceiptNumberService = ReceiptNumberService.Instance

        Public Shared ReadOnly Property Instance As TransactionHistoryService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
        End Sub

        Public Function GetSales(fromDate As Date, toDateExclusive As Date) As List(Of SaleRecord)
            ' Exclude demo seed receipts (OR-001..OR-008) so week/month views only show real issued sales.
            Dim inMemory = _store.Sales.
                Where(Function(s) s.SaleDate >= fromDate AndAlso s.SaleDate < toDateExclusive AndAlso Not IsSampleReceiptNumber(s.ReceiptNumber)).
                ToList()
            Dim inMemoryKeys = New HashSet(Of String)(inMemory.Select(Function(s) s.ReceiptNumber), StringComparer.OrdinalIgnoreCase)

            Dim persisted = _receipts.GetPersistedSales(fromDate, toDateExclusive).
                Where(Function(s) Not inMemoryKeys.Contains(s.ReceiptNumber) AndAlso Not IsSampleReceiptNumber(s.ReceiptNumber)).
                ToList()

            Return inMemory.Concat(persisted).
                OrderByDescending(Function(s) s.SaleDate).
                ThenByDescending(Function(s) ParseReceiptSequence(s.ReceiptNumber)).
                ThenByDescending(Function(s) s.SaleId).
                ToList()
        End Function

        ''' <summary>Demo seeds use short OR-00x; real issued receipts use OR-00001 (5 digits).</summary>
        Private Shared Function IsSampleReceiptNumber(receiptNumber As String) As Boolean
            If String.IsNullOrWhiteSpace(receiptNumber) Then Return False
            Dim match = System.Text.RegularExpressions.Regex.Match(
                receiptNumber.Trim(),
                "^OR-(\d+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If Not match.Success Then Return False
            Return match.Groups(1).Value.Length <= 3
        End Function

        Private Shared Function ParseReceiptSequence(receiptNumber As String) As Integer
            If String.IsNullOrWhiteSpace(receiptNumber) Then Return 0
            Dim digits = New String(receiptNumber.Where(Function(c) Char.IsDigit(c)).ToArray())
            Dim value As Integer
            If Integer.TryParse(digits, value) Then Return value
            Return 0
        End Function

        Public Function ResolveReceiptModel(sale As SaleRecord) As ReceiptModel
            If sale Is Nothing Then Return Nothing

            If sale.Lines IsNot Nothing AndAlso sale.Lines.Count > 0 Then
                Return ReceiptModelMapper.FromSaleRecord(sale)
            End If

            Dim loaded = _receipts.GetReceiptByOrNumber(sale.ReceiptNumber)
            If loaded IsNot Nothing Then Return loaded

            Return ReceiptModelMapper.FromSaleRecord(sale)
        End Function
    End Class
End Namespace
