Namespace Models
    Public Class AppSettings
        Public Property PrinterType As String = "Standard"
        Public Property ThermalPrinterName As String = String.Empty
        Public Property SalonName As String = "Cindy Hair Salon"
        Public Property SalonAddress As String = "123 Beauty Street, Quezon City, Metro Manila"
        Public Property SalonTelephone As String = "(02) 8123-4567"
        Public Property SalonTin As String = "123-456-789-00000"
    End Class

    Public Class IssuedReceiptRecord
        Public Property OrNumber As String = String.Empty
        Public Property OrSequence As Integer
        Public Property SaleId As Integer
        Public Property IssuedAt As DateTime
        Public Property CashierName As String = String.Empty
        Public Property Total As Decimal
    End Class
End Namespace
