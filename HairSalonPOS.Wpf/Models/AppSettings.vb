Namespace Models
    Public Class AppSettings
        Public Property PrinterType As String = "Standard"
        Public Property ThermalPrinterName As String = String.Empty
        Public Property SalonName As String = "Fix Republic"
        Public Property SalonAddress As String = "123 Beauty Street, Quezon City, Metro Manila"
        Public Property SalonTelephone As String = "(02) 8123-4567"
        Public Property SalonTin As String = "123-456-789-00000"
        Public Property IsDarkMode As Boolean
    End Class

    Public Class IssuedReceiptRecord
        Public Property OrNumber As String = String.Empty
        Public Property OrSequence As Integer
        Public Property SaleId As Integer
        Public Property IssuedAt As DateTime
        Public Property CashierName As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property StylistName As String = String.Empty
        Public Property PaymentMethod As String = String.Empty
        Public Property SubTotal As Decimal
        Public Property Discount As Decimal
        Public Property Tax As Decimal
        Public Property Total As Decimal
        Public Property ReceiptJson As String = String.Empty
    End Class
End Namespace
