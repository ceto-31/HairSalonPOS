Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class DiscountsViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private _editCode As String = String.Empty
        Private _editDescription As String = String.Empty
        Private _editType As String = "Percent"
        Private _editValue As Decimal
        Private _editSeniorPwd As Boolean
        Private _isEditMode As Boolean
        Private _statusMessage As String = String.Empty

        Public Sub New()
            Discounts = New ObservableCollection(Of DiscountItem)(_store.Discounts)
            DiscountTypes = New ObservableCollection(Of String) From {"Percent", "Fixed"}
            NewPromoCommand = New RelayCommand(AddressOf BeginAdd)
            SavePromoCommand = New RelayCommand(AddressOf SavePromo)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteDiscountCommand = New RelayCommand(Of DiscountItem)(AddressOf DeleteDiscount)
        End Sub

        Public Property Discounts As ObservableCollection(Of DiscountItem)
        Public Property DiscountTypes As ObservableCollection(Of String)

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                SetProperty(_isEditMode, value)
            End Set
        End Property

        Public Property EditCode As String
            Get
                Return _editCode
            End Get
            Set(value As String)
                SetProperty(_editCode, value)
            End Set
        End Property

        Public Property EditDescription As String
            Get
                Return _editDescription
            End Get
            Set(value As String)
                SetProperty(_editDescription, value)
            End Set
        End Property

        Public Property EditType As String
            Get
                Return _editType
            End Get
            Set(value As String)
                SetProperty(_editType, value)
            End Set
        End Property

        Public Property EditValue As Decimal
            Get
                Return _editValue
            End Get
            Set(value As Decimal)
                SetProperty(_editValue, value)
            End Set
        End Property

        Public Property EditSeniorPwd As Boolean
            Get
                Return _editSeniorPwd
            End Get
            Set(value As Boolean)
                SetProperty(_editSeniorPwd, value)
            End Set
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property NewPromoCommand As RelayCommand
        Public Property SavePromoCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteDiscountCommand As RelayCommand(Of DiscountItem)

        Private Sub BeginAdd()
            IsEditMode = True
            EditCode = String.Empty
            EditDescription = String.Empty
            EditType = "Percent"
            EditValue = 0D
            EditSeniorPwd = False
        End Sub

        Private Sub SavePromo()
            If String.IsNullOrWhiteSpace(EditCode) Then
                StatusMessage = "Promo code is required."
                Return
            End If
            Dim item As New DiscountItem With {
                .Code = EditCode.Trim().ToUpper(),
                .Description = EditDescription.Trim(),
                .DiscountType = EditType,
                .Value = EditValue,
                .IsSeniorPwd = EditSeniorPwd,
                .IsActive = True
            }
            _store.Discounts.Add(item)
            Discounts.Add(item)
            IsEditMode = False
            StatusMessage = "Promo created."
        End Sub

        Private Sub DeleteDiscount(item As DiscountItem)
            If item Is Nothing Then Return
            Dim confirm = System.Windows.MessageBox.Show(
                $"Delete promo '{item.Code}'?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

            _store.Discounts.Remove(item)
            Discounts.Remove(item)
            StatusMessage = $"Promo {item.Code} deleted."
        End Sub
    End Class
End Namespace
