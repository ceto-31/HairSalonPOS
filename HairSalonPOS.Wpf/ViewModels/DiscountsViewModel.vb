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
        Private _isAdding As Boolean = True
        Private _originalCode As String = String.Empty
        Private _statusMessage As String = String.Empty
        Private _showArchived As Boolean
        Private _searchText As String = String.Empty

        Public Sub New()
            DiscountTypes = New ObservableCollection(Of String) From {"Percent", "Fixed"}
            NewPromoCommand = New RelayCommand(AddressOf BeginAdd)
            EditDiscountCommand = New RelayCommand(Of DiscountItem)(AddressOf BeginEdit)
            SavePromoCommand = New RelayCommand(AddressOf SavePromo)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteDiscountCommand = New RelayCommand(Of DiscountItem)(AddressOf DeleteDiscount)
            ArchiveDiscountCommand = New RelayCommand(Of DiscountItem)(AddressOf ArchiveDiscount)
            UnarchiveDiscountCommand = New RelayCommand(Of DiscountItem)(AddressOf UnarchiveDiscount)
            ToggleShowArchivedCommand = New RelayCommand(AddressOf ToggleShowArchived)
            LoadDiscounts()
        End Sub

        Public Property Discounts As ObservableCollection(Of DiscountItem)
        Public Property DiscountTypes As ObservableCollection(Of String)

        Public Property ShowArchived As Boolean
            Get
                Return _showArchived
            End Get
            Set(value As Boolean)
                If SetProperty(_showArchived, value) Then
                    LoadDiscounts()
                    OnPropertyChanged(NameOf(ShowArchivedLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property ShowArchivedLabel As String
            Get
                Return If(ShowArchived, "Hide archived", "Show archived")
            End Get
        End Property

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then
                    LoadDiscounts()
                End If
            End Set
        End Property

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                SetProperty(_isEditMode, value)
            End Set
        End Property

        Public ReadOnly Property FormTitle As String
            Get
                Return If(_isAdding, "New promo", "Edit promo")
            End Get
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
        Public Property EditDiscountCommand As RelayCommand(Of DiscountItem)
        Public Property SavePromoCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteDiscountCommand As RelayCommand(Of DiscountItem)
        Public Property ArchiveDiscountCommand As RelayCommand(Of DiscountItem)
        Public Property UnarchiveDiscountCommand As RelayCommand(Of DiscountItem)
        Public Property ToggleShowArchivedCommand As RelayCommand

        Private Sub ToggleShowArchived()
            ShowArchived = Not ShowArchived
        End Sub

        Private Sub LoadDiscounts()
            Dim query = _store.Discounts.AsEnumerable()
            If ShowArchived Then
                query = query.Where(Function(d) Not d.IsActive)
            Else
                query = query.Where(Function(d) d.IsActive)
            End If
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                query = query.Where(Function(d) d.Code.ToLowerInvariant().Contains(term) OrElse
                    (Not String.IsNullOrWhiteSpace(d.Description) AndAlso d.Description.ToLowerInvariant().Contains(term)))
            End If
            Discounts = New ObservableCollection(Of DiscountItem)(query.OrderBy(Function(d) d.Code))
            OnPropertyChanged(NameOf(Discounts))
        End Sub

        Private Sub BeginAdd()
            _isAdding = True
            _originalCode = String.Empty
            EditCode = String.Empty
            EditDescription = String.Empty
            EditType = "Percent"
            EditValue = 0D
            EditSeniorPwd = False
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(item As DiscountItem)
            If item Is Nothing Then Return
            _isAdding = False
            _originalCode = item.Code
            EditCode = item.Code
            EditDescription = item.Description
            EditType = item.DiscountType
            EditValue = item.Value
            EditSeniorPwd = item.IsSeniorPwd
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub SavePromo()
            If String.IsNullOrWhiteSpace(EditCode) Then
                StatusMessage = "Promo code is required."
                Return
            End If

            Dim code = EditCode.Trim().ToUpper()
            If _isAdding Then
                If _store.Discounts.Any(Function(d) d.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Promo code already exists."
                    Return
                End If
                Dim item As New DiscountItem With {
                    .Code = code,
                    .Description = EditDescription.Trim(),
                    .DiscountType = EditType,
                    .Value = EditValue,
                    .IsSeniorPwd = EditSeniorPwd,
                    .IsActive = True
                }
                _store.Discounts.Add(item)
                StatusMessage = "Promo created."
            Else
                Dim existing = _store.Discounts.FirstOrDefault(Function(d) d.Code = _originalCode)
                If existing Is Nothing Then
                    StatusMessage = "Promo not found."
                    Return
                End If
                If Not code.Equals(_originalCode, StringComparison.OrdinalIgnoreCase) AndAlso
                   _store.Discounts.Any(Function(d) d.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) Then
                    StatusMessage = "Promo code already exists."
                    Return
                End If
                existing.Code = code
                existing.Description = EditDescription.Trim()
                existing.DiscountType = EditType
                existing.Value = EditValue
                existing.IsSeniorPwd = EditSeniorPwd
                StatusMessage = "Promo updated."
            End If

            _store.RaiseDiscountsChanged()
            LoadDiscounts()
            IsEditMode = False
        End Sub

        Private Sub DeleteDiscount(item As DiscountItem)
            If item Is Nothing Then Return
            If Not AppDialogService.ConfirmDelete(item.Code) Then Return

            _store.Discounts.Remove(item)
            _store.RaiseDiscountsChanged()
            StatusMessage = $"Promo {item.Code} deleted."
            LoadDiscounts()
        End Sub

        Private Sub ArchiveDiscount(item As DiscountItem)
            If item Is Nothing OrElse Not item.IsActive Then Return
            item.IsActive = False
            StatusMessage = $"{item.Code} archived."
            _store.RaiseDiscountsChanged()
            LoadDiscounts()
        End Sub

        Private Sub UnarchiveDiscount(item As DiscountItem)
            If item Is Nothing OrElse item.IsActive Then Return
            item.IsActive = True
            StatusMessage = $"{item.Code} restored."
            _store.RaiseDiscountsChanged()
            LoadDiscounts()
        End Sub
    End Class
End Namespace
