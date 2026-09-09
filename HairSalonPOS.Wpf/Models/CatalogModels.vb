Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Text.Json.Serialization
Imports CommunityToolkit.Mvvm.ComponentModel
Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Services

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

    Public Enum ServiceConsumableKind
        Fixed = 0
        PickOne = 1
    End Enum

    Public Class ServiceConsumableLine
        Public Property Kind As ServiceConsumableKind = ServiceConsumableKind.Fixed
        Public Property ProductSku As String = String.Empty
        ''' <summary>Quantity used per one service performed.</summary>
        Public Property Quantity As Decimal = 1D
        ''' <summary>PickOne: allowed product SKUs the cashier chooses at POS.</summary>
        Public Property OptionProductSkus As New List(Of String)
    End Class

    Public Class PickOneProductOption
        Inherits ObservableObject

        Private _isSelected As Boolean

        Public Property Sku As String = String.Empty
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

    Public Class FixedConsumableOption
        Inherits ObservableObject

        Private _isSelected As Boolean
        Private _quantity As Decimal = 1D

        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty

        Public Property IsSelected As Boolean
            Get
                Return _isSelected
            End Get
            Set(value As Boolean)
                SetProperty(_isSelected, value)
            End Set
        End Property

        Public Property Quantity As Decimal
            Get
                Return _quantity
            End Get
            Set(value As Decimal)
                SetProperty(_quantity, value)
            End Set
        End Property

        <JsonIgnore>
        Public ReadOnly Property QuantityLabel As String
            Get
                Return $"Qty {Quantity:0.##}"
            End Get
        End Property
    End Class

    Public Class ServiceItem
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Price As Decimal
        Public Property DurationMinutes As Integer
        Public Property MinDurationMinutes As Integer
        Public Property MaxDurationMinutes As Integer
        Public Property Icon As String = "✂️"
        Public Property Category As String = String.Empty
        Public Property SubCategory As String = String.Empty
        ''' <summary>Commission as percent of price. 0 = no commission.</summary>
        Public Property CommissionPercent As Decimal
        Public Property IsActive As Boolean = True
        Public Property Consumables As New List(Of ServiceConsumableLine)

        <JsonIgnore>
        Public ReadOnly Property HasPickOneConsumables As Boolean
            Get
                Return Consumables?.Any(Function(c) c.Kind = ServiceConsumableKind.PickOne) = True
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property ConsumableCountLabel As String
            Get
                If Consumables Is Nothing OrElse Consumables.Count = 0 Then Return "No products linked"
                Dim fixedCount = Consumables.Where(Function(c) c.Kind = ServiceConsumableKind.Fixed).Count()
                Dim pickCount = Consumables.Where(Function(c) c.Kind = ServiceConsumableKind.PickOne).Count()
                If pickCount = 0 Then
                    Return If(fixedCount = 1, "1 product linked", $"{fixedCount} products linked")
                End If
                If fixedCount = 0 Then
                    Return If(pickCount = 1, "1 pick-at-POS", $"{pickCount} pick-at-POS")
                End If
                Return $"{fixedCount} fixed, {pickCount} pick-at-POS"
            End Get
        End Property

        Public ReadOnly Property CommissionDisplay As String
            Get
                If CommissionPercent <= 0D Then Return "None"
                Return $"{CommissionPercent:0.##}%"
            End Get
        End Property

        Public ReadOnly Property StatusLabel As String
            Get
                Return If(IsActive, "Active", "Archived")
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property DurationRangeLabel As String
            Get
                Dim min = EffectiveMinDurationMinutes()
                Dim max = EffectiveMaxDurationMinutes()
                If min <= 0 AndAlso max <= 0 Then Return "—"
                If min = max Then Return $"{min} min"
                Return $"{min}–{max} min"
            End Get
        End Property

        Public Function EffectiveMinDurationMinutes() As Integer
            If MinDurationMinutes > 0 Then Return MinDurationMinutes
            If DurationMinutes > 0 Then Return DurationMinutes
            Return 0
        End Function

        Public Function EffectiveMaxDurationMinutes() As Integer
            If MaxDurationMinutes > 0 Then Return MaxDurationMinutes
            If MinDurationMinutes > 0 Then Return MinDurationMinutes
            If DurationMinutes > 0 Then Return DurationMinutes
            Return 0
        End Function
    End Class

    Public Class ProductItem
        Inherits ObservableObject

        Private _imagePath As String = String.Empty

        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Brand As String = String.Empty
        Public Property Price As Decimal
        Public Property Cost As Decimal
        Private _reorderLevel As Integer = 10

        Public Property ReorderLevel As Integer
            Get
                Return _reorderLevel
            End Get
            Set(value As Integer)
                If SetProperty(_reorderLevel, value) Then
                    NotifyStockPresentationChanged()
                End If
            End Set
        End Property

        Public Property Category As String = String.Empty
        Public Property SubCategory As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property ExpirationDate As Date?

        ''' <summary>How many pieces come in one box/case for this product. 1 = stocked as pieces only.</summary>
        Public Property UnitsPerBox As Integer = 1
        Public Property UnitLabel As String = "pc"
        Public Property BoxLabel As String = "box"
        ''' <summary>Optional per-product override of the global expiration warning window.</summary>
        Public Property ExpirationWarningDays As Integer?

        Public Property ImagePath As String
            Get
                Return _imagePath
            End Get
            Set(value As String)
                If SetProperty(_imagePath, If(value, String.Empty)) Then
                    OnPropertyChanged(NameOf(HasImage))
                End If
            End Set
        End Property

        <JsonIgnore>
        Public ReadOnly Property HasImage As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(ImagePath)
            End Get
        End Property

        Public ReadOnly Property StatusLabel As String
            Get
                Return If(IsActive, "Active", "Archived")
            End Get
        End Property

        ''' <summary>Legacy JSON field — migrated into StockBatch on first run; not used for reads after migration.</summary>
        <JsonPropertyName("StockOnHand")>
        Public Property StockOnHandLegacy As Integer

        ''' <summary>Legacy JSON field — migrated into StockBatch on first run; not used for reads after migration.</summary>
        <JsonPropertyName("ReservedQty")>
        Public Property ReservedQtyLegacy As Integer

        <JsonIgnore>
        Public ReadOnly Property StockOnHand As Integer
            Get
                If String.IsNullOrWhiteSpace(Sku) Then Return Math.Max(0, StockOnHandLegacy)
                Return InMemoryDataStore.Instance.StockOnHandForSku(Sku)
            End Get
        End Property

        ''' <summary>Reserve stock — emergency backup separate from daily on-hand; used only with confirmation at checkout.</summary>
        <JsonIgnore>
        Public ReadOnly Property ReservedQty As Integer
            Get
                If String.IsNullOrWhiteSpace(Sku) Then Return Math.Max(0, ReservedQtyLegacy)
                Return InMemoryDataStore.Instance.ReservedQtyForSku(Sku)
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property AvailableQty As Integer
            Get
                Return Math.Max(0, StockOnHand)
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property StockSummaryLabel As String
            Get
                If ReservedQty <= 0 Then Return $"{StockOnHand} on hand"
                Return $"{StockOnHand} on hand · {ReservedQty} reserve stock"
            End Get
        End Property

        ''' <summary>Legacy flag: true when on hand is at or below reorder (includes Out).</summary>
        <JsonIgnore>
        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return StockOnHand <= ReorderLevel
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsOutOfStock As Boolean
            Get
                Return StockOnHand <= 0
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsStockLow As Boolean
            Get
                Return StockOnHand > 0 AndAlso StockOnHand <= ReorderLevel
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsStockOk As Boolean
            Get
                Return StockOnHand > ReorderLevel
            End Get
        End Property

        ''' <summary>OK, Low, or Out — used for pills and styling.</summary>
        <JsonIgnore>
        Public ReadOnly Property StockStatus As String
            Get
                If IsOutOfStock Then Return "Out"
                If IsStockLow Then Return "Low"
                Return "OK"
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property Status As String
            Get
                Return StockStatus
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property StockShortfall As Integer
            Get
                If StockOnHand >= ReorderLevel Then Return 0
                Return ReorderLevel - StockOnHand
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property SuggestedOrderQty As Integer
            Get
                Return Math.Max(1, StockShortfall)
            End Get
        End Property

        Public ReadOnly Property ShowStockWarning As Boolean
            Get
                Return IsOutOfStock OrElse IsStockLow
            End Get
        End Property

        Public ReadOnly Property StockWarningMessage As String
            Get
                If IsOutOfStock Then
                    Return $"Out of stock. Your reorder point is {ReorderLevel}."
                End If
                If IsStockLow Then
                    Return $"{StockOnHand} on hand, {StockShortfall} below your reorder point of {ReorderLevel}."
                End If
                Return String.Empty
            End Get
        End Property

        ''' <summary>0–1 fill ratio for stock level bar (relative to reorder point).</summary>
        Public ReadOnly Property StockLevelFillRatio As Double
            Get
                If ReorderLevel <= 0 Then Return If(StockOnHand > 0, 1.0R, 0.0R)
                If StockOnHand <= 0 Then Return 0.0R
                Return Math.Min(1.0R, StockOnHand / ReorderLevel)
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property PlaceholderIcon As String
            Get
                Return ProductPlaceholderIcons.Resolve(Me)
            End Get
        End Property

        Public Sub RefreshStockPresentation()
            OnPropertyChanged(NameOf(StockOnHand))
            OnPropertyChanged(NameOf(ReservedQty))
            NotifyStockPresentationChanged()
        End Sub

        Private Sub NotifyStockPresentationChanged()
            OnPropertyChanged(NameOf(AvailableQty))
            OnPropertyChanged(NameOf(StockSummaryLabel))
            OnPropertyChanged(NameOf(Status))
            OnPropertyChanged(NameOf(StockStatus))
            OnPropertyChanged(NameOf(IsLowStock))
            OnPropertyChanged(NameOf(IsOutOfStock))
            OnPropertyChanged(NameOf(IsStockLow))
            OnPropertyChanged(NameOf(IsStockOk))
            OnPropertyChanged(NameOf(StockShortfall))
            OnPropertyChanged(NameOf(SuggestedOrderQty))
            OnPropertyChanged(NameOf(ShowStockWarning))
            OnPropertyChanged(NameOf(StockWarningMessage))
            OnPropertyChanged(NameOf(StockLevelFillRatio))
        End Sub

        ''' <summary>Coalesce null/legacy JSON values so inventory and stock dialogs never hit null fields.</summary>
        Public Sub EnsureDefaults()
            Sku = If(Sku, String.Empty).Trim()
            Name = If(Name, String.Empty).Trim()
            Brand = If(Brand, String.Empty).Trim()
            Category = If(Category, String.Empty).Trim()
            SubCategory = If(SubCategory, String.Empty).Trim()
            ImagePath = If(ImagePath, String.Empty).Trim()
            If ReorderLevel <= 0 Then ReorderLevel = 10
            If UnitsPerBox <= 0 Then UnitsPerBox = 1
            If UnitLabel Is Nothing Then UnitLabel = "pc"
            If BoxLabel Is Nothing Then BoxLabel = "box"
            If StockOnHandLegacy < 0 Then StockOnHandLegacy = 0
            If ReservedQtyLegacy < 0 Then ReservedQtyLegacy = 0
        End Sub
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

        Private _stylistName As String = String.Empty
        Public Property StylistName As String
            Get
                Return _stylistName
            End Get
            Set(value As String)
                SetProperty(_stylistName, If(value, String.Empty))
            End Set
        End Property

        Public Property ConsumableSelections As New List(Of ServiceConsumableLine)

        Public Property ConsumableSummary As String = String.Empty

        <JsonIgnore>
        Public ReadOnly Property CartDisplayName As String
            Get
                If String.IsNullOrWhiteSpace(ConsumableSummary) Then Return Name
                Return $"{Name} · {ConsumableSummary}"
            End Get
        End Property

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
        Public Property StylistName As String = String.Empty
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
