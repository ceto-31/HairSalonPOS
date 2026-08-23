Imports System.Collections.ObjectModel
Imports System.Text.Json.Serialization
Imports System.Windows.Media
Imports CommunityToolkit.Mvvm.ComponentModel

Namespace Models
    Public Class StaffMember
        Inherits ObservableObject

        Private _imagePath As String = String.Empty

        Public Property StaffId As Integer
        Public Property Name As String = String.Empty
        Public Property Role As String = String.Empty
        Public Property ContactNumber As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property IsActive As Boolean = True
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

    Public Class AppointmentStatuses
        Public Const Scheduled As String = "Scheduled"
        Public Const Confirmed As String = "Confirmed"
        Public Const Done As String = "Done"
        Public Const NoShow As String = "NoShow"
    End Class

    Public Class AppointmentItem
        Inherits ObservableObject

        Private _appointmentId As Integer
        Private _customerName As String = String.Empty
        Private _staffName As String = String.Empty
        Private _serviceName As String = String.Empty
        Private _startTime As DateTime
        Private _durationMinutes As Integer
        Private _status As String = AppointmentStatuses.Scheduled
        Private _contactNumber As String = String.Empty
        Private _email As String = String.Empty
        Private _completedAt As DateTime?

        Public Property AppointmentId As Integer
            Get
                Return _appointmentId
            End Get
            Set(value As Integer)
                SetProperty(_appointmentId, value)
            End Set
        End Property

        Public Property CustomerName As String
            Get
                Return _customerName
            End Get
            Set(value As String)
                SetProperty(_customerName, value)
            End Set
        End Property

        Public Property StaffName As String
            Get
                Return _staffName
            End Get
            Set(value As String)
                SetProperty(_staffName, value)
            End Set
        End Property

        Public Property ServiceName As String
            Get
                Return _serviceName
            End Get
            Set(value As String)
                SetProperty(_serviceName, value)
            End Set
        End Property

        Public Property StartTime As DateTime
            Get
                Return _startTime
            End Get
            Set(value As DateTime)
                If SetProperty(_startTime, value) Then
                    OnPropertyChanged(NameOf(TimeLabel))
                    OnPropertyChanged(NameOf(EndTime))
                End If
            End Set
        End Property

        Public Property DurationMinutes As Integer
            Get
                Return _durationMinutes
            End Get
            Set(value As Integer)
                If SetProperty(_durationMinutes, value) Then
                    OnPropertyChanged(NameOf(EndTime))
                End If
            End Set
        End Property

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

        Public Property Status As String
            Get
                Return _status
            End Get
            Set(value As String)
                If SetProperty(_status, If(value, AppointmentStatuses.Scheduled)) Then
                    OnPropertyChanged(NameOf(StatusLabel))
                    OnPropertyChanged(NameOf(DisplayStatusLabel))
                    OnPropertyChanged(NameOf(IsScheduled))
                    OnPropertyChanged(NameOf(IsConfirmed))
                    OnPropertyChanged(NameOf(IsOpen))
                    OnPropertyChanged(NameOf(IsDone))
                    OnPropertyChanged(NameOf(IsNoShow))
                    OnPropertyChanged(NameOf(IsPastDue))
                End If
            End Set
        End Property

        Public Property ContactNumber As String
            Get
                Return _contactNumber
            End Get
            Set(value As String)
                SetProperty(_contactNumber, If(value, String.Empty))
            End Set
        End Property

        Public Property Email As String
            Get
                Return _email
            End Get
            Set(value As String)
                SetProperty(_email, If(value, String.Empty))
            End Set
        End Property

        Public Property CompletedAt As DateTime?
            Get
                Return _completedAt
            End Get
            Set(value As DateTime?)
                SetProperty(_completedAt, value)
            End Set
        End Property

        Public ReadOnly Property StatusLabel As String
            Get
                Select Case Status
                    Case AppointmentStatuses.Done
                        Return "Done"
                    Case AppointmentStatuses.Confirmed
                        Return "Confirmed"
                    Case AppointmentStatuses.NoShow
                        Return "Cancelled"
                    Case Else
                        Return "Pending"
                End Select
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property DisplayStatusLabel As String
            Get
                Select Case Status
                    Case AppointmentStatuses.Done
                        Return "Done"
                    Case AppointmentStatuses.Confirmed
                        Return "Confirmed"
                    Case AppointmentStatuses.NoShow
                        Return "Cancelled"
                    Case Else
                        Return "Pending"
                End Select
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsScheduled As Boolean
            Get
                Return Status = AppointmentStatuses.Scheduled
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsConfirmed As Boolean
            Get
                Return Status = AppointmentStatuses.Confirmed
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsOpen As Boolean
            Get
                Return Status = AppointmentStatuses.Scheduled OrElse Status = AppointmentStatuses.Confirmed
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsDone As Boolean
            Get
                Return Status = AppointmentStatuses.Done
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsNoShow As Boolean
            Get
                Return Status = AppointmentStatuses.NoShow
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsPastDue As Boolean
            Get
                Return IsOpen AndAlso EndTime < DateTime.Now
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
        Public Property IsActive As Boolean = True

        Public ReadOnly Property StatusLabel As String
            Get
                Return If(IsActive, "Active", "Archived")
            End Get
        End Property

        Public ReadOnly Property SubCategorySummary As String
            Get
                Dim count = If(SubCategories?.Count, 0)
                If count = 0 Then Return "No subcategories"
                If count = 1 Then Return "1 subcategory"
                Return $"{count} subcategories"
            End Get
        End Property
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

    Public Class DashboardAppointmentRow
        Public Property TimeLabel As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property ServiceName As String = String.Empty
        Public Property StaffName As String = String.Empty
        Public Property StatusLabel As String = String.Empty
        Public Property IsConfirmed As Boolean
        Public ReadOnly Property StaffInitials As String
            Get
                Dim parts = StaffName.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length = 0 Then Return "?"
                Return String.Join("", parts.Take(2).Select(Function(p) p(0).ToString())).ToUpper()
            End Get
        End Property
    End Class

    Public Class AppointmentHistoryRow
        Public Property AppointmentId As Integer
        Public Property DateLabel As String = String.Empty
        Public Property TimeLabel As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property ServiceName As String = String.Empty
        Public Property StaffLabel As String = String.Empty
        Public Property StatusLabel As String = String.Empty
        Public Property AmountLabel As String = String.Empty
        Public Property SourceAppointment As AppointmentItem
    End Class

    Public Class DashboardSaleRow
        Public Property ReceiptNumber As String = String.Empty
        Public Property TimeLabel As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property Total As Decimal
    End Class

    Public Class LowStockAlertRow
        Public Property Sku As String = String.Empty
        Public Property ProductName As String = String.Empty
        Public Property StockOnHand As Integer
        Public Property ReorderLevel As Integer
        Public Property ImagePath As String = String.Empty
    End Class

    Public Class DashboardStaffPerformanceRow
        Public Property Rank As Integer
        Public Property StaffName As String = String.Empty
        Public Property ImagePath As String = String.Empty
        Public Property ServicesCompleted As Integer

        Public ReadOnly Property IsTopPerformer As Boolean
            Get
                Return Rank > 0 AndAlso Rank <= 3
            End Get
        End Property

        Public ReadOnly Property RankLabel As String
            Get
                If IsTopPerformer Then Return $"#{Rank}"
                Return Rank.ToString()
            End Get
        End Property

        Public ReadOnly Property HasImage As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(ImagePath)
            End Get
        End Property

        Public ReadOnly Property Initials As String
            Get
                Dim parts = StaffName.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length = 0 Then Return "?"
                Return String.Join("", parts.Take(2).Select(Function(p) p(0).ToString())).ToUpper()
            End Get
        End Property
    End Class

    Public Class DashboardTopServiceRow
        Public Property Name As String = String.Empty
        Public Property Amount As Decimal
        Public Property BarWidth As Double
    End Class

    Public Class DashboardDonutSlice
        Public Property Label As String = String.Empty
        Public Property Amount As Decimal
        Public Property PercentLabel As String = String.Empty
        Public Property AmountLabel As String = String.Empty
        Public Property SliceBrush As Brush
        Public Property SliceGeometry As PathGeometry
    End Class

    Public Class DashboardChartPoint
        Public Property Label As String = String.Empty
        Public Property Amount As Decimal
        Public Property X As Double
        Public Property Y As Double
        Public Property MarkerLeft As Double
        Public Property MarkerTop As Double
        Public Property BarLeft As Double
        Public Property BarTop As Double
        Public Property BarWidth As Double
        Public Property BarHeight As Double
        Public Property IsEmphasis As Boolean
        Public Property ShowLabel As Boolean = True
        Public Property BarOpacity As Double = 0.85
    End Class

    Public Class DashboardLineChart
        Public Property Title As String = String.Empty
        Public Property Subtitle As String = String.Empty
        Public Property ChartKind As String = "Area"
        Public Property ChartWidth As Double = 304
        Public Property CurveGeometry As PathGeometry
        Public Property AreaGeometry As PathGeometry
        Public Property MaxAmountLabel As String = String.Empty
        Public Property Points As ObservableCollection(Of DashboardChartPoint)

        Public ReadOnly Property IsBarChart As Boolean
            Get
                Return ChartKind = "Bar"
            End Get
        End Property

        Public ReadOnly Property IsAreaChart As Boolean
            Get
                Return ChartKind <> "Bar"
            End Get
        End Property
    End Class
End Namespace
