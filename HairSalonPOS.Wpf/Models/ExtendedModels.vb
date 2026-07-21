Imports CommunityToolkit.Mvvm.ComponentModel

Namespace Models
    Public Class CustomerItem
        Inherits ObservableObject

        Public Property CustomerId As Integer
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property VisitCount As Integer
        Public Property LoyaltyPoints As Integer

        Public ReadOnly Property Initials As String
            Get
                Dim parts = Name.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length = 0 Then Return "?"
                If parts.Length = 1 Then Return parts(0).Substring(0, Math.Min(2, parts(0).Length)).ToUpper()
                Return (parts(0)(0).ToString() & parts(parts.Length - 1)(0).ToString()).ToUpper()
            End Get
        End Property
    End Class

    Public Class StaffMember
        Inherits ObservableObject

        Public Property StaffId As Integer
        Public Property Name As String = String.Empty
        Public Property Role As String = String.Empty
        Public Property CommissionRate As Decimal
        Public Property IsActive As Boolean = True

        Public ReadOnly Property Initials As String
            Get
                Dim parts = Name.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length = 0 Then Return "?"
                Return String.Join("", parts.Take(2).Select(Function(p) p(0).ToString())).ToUpper()
            End Get
        End Property
    End Class

    Public Class PackageItem
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Price As Decimal
        Public Property Icon As String = "📦"
        Public Property IncludedSkus As New List(Of String)
    End Class

    Public Class DiscountItem
        Public Property Code As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property DiscountType As String = "Percent"
        Public Property Value As Decimal
        Public Property IsSeniorPwd As Boolean
        Public Property IsActive As Boolean = True
        Public Property EndDate As Date?

        Public ReadOnly Property StatusLabel As String
            Get
                If EndDate.HasValue AndAlso EndDate.Value < Date.Today Then Return "Expired"
                If EndDate.HasValue AndAlso EndDate.Value <= Date.Today.AddDays(30) Then Return "Expiring"
                Return "Active"
            End Get
        End Property
    End Class

    Public Class AppointmentItem
        Public Property AppointmentId As Integer
        Public Property CustomerName As String = String.Empty
        Public Property StaffName As String = String.Empty
        Public Property ServiceName As String = String.Empty
        Public Property StartTime As DateTime
        Public Property DurationMinutes As Integer

        Public ReadOnly Property TimeLabel As String
            Get
                Return StartTime.ToString("h:mm tt")
            End Get
        End Property

        Public ReadOnly Property EndTime As DateTime
            Get
                Return StartTime.AddMinutes(DurationMinutes)
            End Get
        End Property
    End Class

    Public Class StockMovement
        Public Property MovementId As Integer
        Public Property Sku As String = String.Empty
        Public Property ProductName As String = String.Empty
        Public Property ChangeQty As Integer
        Public Property MovementType As String = String.Empty
        Public Property UserName As String = String.Empty
        Public Property CreatedAt As DateTime
        Public Property Notes As String = String.Empty
    End Class

    Public Class CatalogTile
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Price As Decimal
        Public Property Icon As String = "•"
        Public Property TileType As String = "Service"
        Public Property Category As String = String.Empty
        Public Property SubCategory As String = String.Empty
    End Class

    Public Class CatalogCategoryNode
        Public Property Name As String = String.Empty
        Public Property SubCategories As New List(Of String)
    End Class

    Public Class SelectableChip
        Inherits ObservableObject

        Private _isSelected As Boolean

        Public Property Name As String = String.Empty

        Public Property IsSelected As Boolean
            Get
                Return _isSelected
            End Get
            Set(value As Boolean)
                SetProperty(_isSelected, value)
            End Set
        End Property
    End Class

    Public Class RevenueBarItem
        Public Property Label As String = String.Empty
        Public Property Amount As Decimal
        Public Property BarHeight As Double
    End Class

    Public Class StylistPerformanceItem
        Public Property StylistName As String = String.Empty
        Public Property ServiceCount As Integer
        Public Property Revenue As Decimal
    End Class
End Namespace
