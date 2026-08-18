Imports CommunityToolkit.Mvvm.Input

Namespace ViewModels
    Public Class MasterFilesViewModel
        Inherits ViewModelBase

        Private _section As String = "Categories"

        Public Sub New()
            Services = New ServicesViewModel() With {.IsHostedInMasterFiles = True}
            Products = New ProductsCatalogViewModel()
            Staff = New StaffViewModel() With {.IsHostedInMasterFiles = True}
            Discounts = New DiscountsViewModel()

            ShowCategoriesCommand = New RelayCommand(Sub() Section = "Categories")
            ShowServicesCommand = New RelayCommand(Sub() Section = "Services")
            ShowProductsCommand = New RelayCommand(Sub() Section = "Products")
            ShowStaffCommand = New RelayCommand(Sub() Section = "Staff")
            ShowDiscountsCommand = New RelayCommand(Sub() Section = "Discounts")

            ApplySection()
        End Sub

        Public Property Services As ServicesViewModel
        Public Property Products As ProductsCatalogViewModel
        Public Property Staff As StaffViewModel
        Public Property Discounts As DiscountsViewModel

        Public Property Section As String
            Get
                Return _section
            End Get
            Set(value As String)
                If SetProperty(_section, value) Then
                    ApplySection()
                    OnPropertyChanged(NameOf(IsCategories))
                    OnPropertyChanged(NameOf(IsServices))
                    OnPropertyChanged(NameOf(IsProducts))
                    OnPropertyChanged(NameOf(IsStaff))
                    OnPropertyChanged(NameOf(IsDiscounts))
                End If
            End Set
        End Property

        Public ReadOnly Property IsCategories As Boolean
            Get
                Return Section = "Categories"
            End Get
        End Property

        Public ReadOnly Property IsServices As Boolean
            Get
                Return Section = "Services"
            End Get
        End Property

        Public ReadOnly Property IsProducts As Boolean
            Get
                Return Section = "Products"
            End Get
        End Property

        Public ReadOnly Property IsStaff As Boolean
            Get
                Return Section = "Staff"
            End Get
        End Property

        Public ReadOnly Property IsDiscounts As Boolean
            Get
                Return Section = "Discounts"
            End Get
        End Property

        Public Property ShowCategoriesCommand As RelayCommand
        Public Property ShowServicesCommand As RelayCommand
        Public Property ShowProductsCommand As RelayCommand
        Public Property ShowStaffCommand As RelayCommand
        Public Property ShowDiscountsCommand As RelayCommand

        Public Sub LoadFromStore()
            ApplySection()
        End Sub

        Private Sub ApplySection()
            Select Case Section
                Case "Categories"
                    Services.EnterCategorySection()
                Case "Services"
                    Services.EnterServicesSection()
                Case "Products"
                    Services.LeaveMasterSection()
                    Products.LoadFromStore()
                Case "Staff"
                    Services.LeaveMasterSection()
                    Staff.LoadFromStore()
                Case "Discounts"
                    Services.LeaveMasterSection()
            End Select
        End Sub
    End Class
End Namespace
