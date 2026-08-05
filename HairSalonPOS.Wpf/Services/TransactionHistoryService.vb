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
            Dim inMemory = _store.Sales.Where(Function(s) s.SaleDate >= fromDate AndAlso s.SaleDate < toDateExclusive).ToList()
            Dim inMemoryKeys = New HashSet(Of String)(inMemory.Select(Function(s) s.ReceiptNumber), StringComparer.OrdinalIgnoreCase)

            Dim persisted = _receipts.GetPersistedSales(fromDate, toDateExclusive).
                Where(Function(s) Not inMemoryKeys.Contains(s.ReceiptNumber)).
                ToList()

            Return inMemory.Concat(persisted).OrderByDescending(Function(s) s.SaleDate).ToList()
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
