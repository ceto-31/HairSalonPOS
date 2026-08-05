Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class StaffViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private _editFirstName As String = String.Empty
        Private _editLastName As String = String.Empty
        Private _editRole As String = "Stylist"
        Private _editCommission As Decimal = 8D
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingStaffId As Integer
        Private _statusMessage As String = String.Empty

        Public Sub New()
            StaffMembers = New ObservableCollection(Of StaffMember)(_store.Staff)
            AddStaffCommand = New RelayCommand(AddressOf BeginAdd)
            EditStaffCommand = New RelayCommand(Of StaffMember)(AddressOf BeginEdit)
            SaveStaffCommand = New RelayCommand(AddressOf SaveStaff)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteStaffCommand = New RelayCommand(Of StaffMember)(AddressOf DeleteStaff)
        End Sub

        Public Property StaffMembers As ObservableCollection(Of StaffMember)

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
                Return If(_isAdding, "Add staff member", "Edit staff member")
            End Get
        End Property

        Public Property EditFirstName As String
            Get
                Return _editFirstName
            End Get
            Set(value As String)
                SetProperty(_editFirstName, value)
            End Set
        End Property

        Public Property EditLastName As String
            Get
                Return _editLastName
            End Get
            Set(value As String)
                SetProperty(_editLastName, value)
            End Set
        End Property

        Public Property EditRole As String
            Get
                Return _editRole
            End Get
            Set(value As String)
                SetProperty(_editRole, value)
            End Set
        End Property

        Public Property EditCommission As Decimal
            Get
                Return _editCommission
            End Get
            Set(value As Decimal)
                SetProperty(_editCommission, value)
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

        Public Property AddStaffCommand As RelayCommand
        Public Property EditStaffCommand As RelayCommand(Of StaffMember)
        Public Property SaveStaffCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteStaffCommand As RelayCommand(Of StaffMember)

        Private Sub BeginAdd()
            _isAdding = True
            _editingStaffId = 0
            EditFirstName = String.Empty
            EditLastName = String.Empty
            EditRole = "Stylist"
            EditCommission = 8D
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(member As StaffMember)
            If member Is Nothing Then Return
            _isAdding = False
            _editingStaffId = member.StaffId
            Dim parts = SplitName(member.Name)
            EditFirstName = parts.Item1
            EditLastName = parts.Item2
            EditRole = member.Role
            EditCommission = member.CommissionRate
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub SaveStaff()
            If String.IsNullOrWhiteSpace(EditFirstName) Then
                StatusMessage = "First name is required."
                Return
            End If
            If String.IsNullOrWhiteSpace(EditLastName) Then
                StatusMessage = "Last name is required."
                Return
            End If

            Dim fullName = $"{EditFirstName.Trim()} {EditLastName.Trim()}"

            If _isAdding Then
                Dim member As New StaffMember With {
                    .StaffId = If(_store.Staff.Count = 0, 1, _store.Staff.Max(Function(s) s.StaffId) + 1),
                    .Name = fullName,
                    .Role = EditRole.Trim(),
                    .CommissionRate = EditCommission,
                    .IsActive = True
                }
                _store.Staff.Add(member)
                StaffMembers.Add(member)
                StatusMessage = "Staff member added."
            Else
                Dim existing = _store.Staff.FirstOrDefault(Function(s) s.StaffId = _editingStaffId)
                If existing Is Nothing Then
                    StatusMessage = "Staff member not found."
                    Return
                End If
                existing.Name = fullName
                existing.Role = EditRole.Trim()
                existing.CommissionRate = EditCommission
                StaffMembers = New ObservableCollection(Of StaffMember)(_store.Staff)
                OnPropertyChanged(NameOf(StaffMembers))
                StatusMessage = "Staff member updated."
            End If

            _store.RaiseStaffChanged()
            IsEditMode = False
        End Sub

        Private Sub DeleteStaff(member As StaffMember)
            If member Is Nothing Then Return
            If Not AppDialogService.Confirm(
                $"Remove {member.Name} from staff? This action cannot be undone.",
                "Delete Item?",
                "Delete",
                "Cancel",
                AppDialogType.Warning) Then Return

            _store.Staff.Remove(member)
            StaffMembers.Remove(member)
            StatusMessage = $"{member.Name} removed."
            _store.RaiseStaffChanged()
        End Sub

        Private Shared Function SplitName(fullName As String) As (String, String)
            If String.IsNullOrWhiteSpace(fullName) Then
                Return (String.Empty, String.Empty)
            End If

            Dim trimmed = fullName.Trim()
            Dim spaceIndex = trimmed.IndexOf(" "c)
            If spaceIndex < 0 Then
                Return (trimmed, String.Empty)
            End If

            Return (trimmed.Substring(0, spaceIndex), trimmed.Substring(spaceIndex + 1).Trim())
        End Function
    End Class
End Namespace
