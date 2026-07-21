Imports CommunityToolkit.Mvvm.ComponentModel

Namespace Models
    Public Class UserAccount
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property Password As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Role As String = String.Empty
        Public Property FavNumber As String = String.Empty
        Public Property FavColor As String = String.Empty
        Public Property FavAnimal As String = String.Empty
    End Class

    Public Class ServiceItem
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Price As Decimal
        Public Property DurationMinutes As Integer
        Public Property Icon As String = "✂️"
        Public Property Category As String = String.Empty
        Public Property SubCategory As String = String.Empty
    End Class

    Public Class ProductItem
        Inherits ObservableObject

        Private _stockOnHand As Integer

        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Brand As String = String.Empty
        Public Property Price As Decimal
        Public Property Cost As Decimal
        Public Property ReorderLevel As Integer = 10
        Public Property Category As String = String.Empty
        Public Property SubCategory As String = String.Empty

        Public Property StockOnHand As Integer
            Get
                Return _stockOnHand
            End Get
            Set(value As Integer)
                SetProperty(_stockOnHand, value)
                OnPropertyChanged(NameOf(Status))
                OnPropertyChanged(NameOf(IsLowStock))
            End Set
        End Property

        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return StockOnHand <= ReorderLevel
            End Get
        End Property

        Public ReadOnly Property Status As String
            Get
                Return If(IsLowStock, "Low stock", "OK")
            End Get
        End Property
    End Class

    Public Class CartLine
        Inherits ObservableObject

        Public Property LineId As Guid = Guid.NewGuid()
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property UnitPrice As Decimal

        Private _quantity As Integer = 1
        Public Property Quantity As Integer
            Get
                Return _quantity
            End Get
            Set(value As Integer)
                SetProperty(_quantity, value)
                OnPropertyChanged(NameOf(LineTotal))
            End Set
        End Property

        Public Property IsService As Boolean

        Public ReadOnly Property LineTotal As Decimal
            Get
                Return UnitPrice * Quantity
            End Get
        End Property
    End Class

    Public Class SaleLineRecord
        Public Property Name As String = String.Empty
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property LineTotal As Decimal
        Public Property IsService As Boolean
    End Class

    Public Class SaleRecord
        Public Property SaleId As Integer
        Public Property SaleDate As DateTime
        Public Property CashierName As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property StylistName As String = String.Empty
        Public Property PaymentMethod As String = String.Empty
        Public Property SubTotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property Tax As Decimal
        Public Property Total As Decimal
        Public Property PromoCode As String = String.Empty
        Public Property AmountTendered As Decimal
        Public Property ChangeGiven As Decimal
        Public Property ReceiptNumber As String = String.Empty
        Public Property Lines As New List(Of SaleLineRecord)
    End Class

    Public Class ReceiptModel
        Public Property SaleId As Integer
        Public Property ReceiptNumber As String = String.Empty
        Public Property SaleDate As DateTime
        Public Property CashierName As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property StylistName As String = String.Empty
        Public Property PaymentMethod As String = String.Empty
        Public Property SubTotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property DiscountLabel As String = String.Empty
        Public Property PromoCode As String = String.Empty
        Public Property VatableSales As Decimal
        Public Property Tax As Decimal
        Public Property Total As Decimal
        Public Property AmountTendered As Decimal
        Public Property ChangeGiven As Decimal
        Public Property AllLines As New List(Of SaleLineRecord)
        Public Property ServiceLines As New List(Of SaleLineRecord)
        Public Property ProductLines As New List(Of SaleLineRecord)

        Public ReadOnly Property DisplayCustomerName As String
            Get
                Return If(String.IsNullOrWhiteSpace(CustomerName), "Walk-in", CustomerName)
            End Get
        End Property
    End Class
End Namespace
