Namespace Services
    Public Enum StockMovementKind
        StockIn = 0
        StockOut = 1
        Reserve = 2
    End Enum

    Public Class StockMovementStockOutOptions
        Public Property FromReserve As Boolean
        Public Property PreselectedBatchId As Integer?
        Public Property LockBatchSelection As Boolean = False
        Public Property DefaultReason As String = "Expired"
    End Class
End Namespace
