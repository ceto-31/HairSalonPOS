Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class StaffViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private _editName As String = String.Empty
        Private _editRole As String = "Stylist"
        Private _editCommission As Decimal = 8D
        Private _isEditMode As Boolean
        Private _statusMessage As String = String.Empty

        Public Sub New()
            StaffMembers = New ObservableCollection(Of StaffMember)(_store.Staff)
            AddStaffCommand = New RelayCommand(AddressOf BeginAdd)
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

        Public Property EditName As String
            Get
                Return _editName
            End Get
            Set(value As String)
                SetProperty(_editName, value)
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
        Public Property SaveStaffCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteStaffCommand As RelayCommand(Of StaffMember)

        Private Sub BeginAdd()
            IsEditMode = True
            EditName = String.Empty
            EditRole = "Stylist"
            EditCommission = 8D
        End Sub

        Private Sub SaveStaff()
            If String.IsNullOrWhiteSpace(EditName) Then
                StatusMessage = "Name is required."
                Return
            End If
            Dim member As New StaffMember With {
                .StaffId = _store.Staff.Count + 1,
                .Name = EditName.Trim(),
                .Role = EditRole.Trim(),
                .CommissionRate = EditCommission,
                .IsActive = True
            }
            _store.Staff.Add(member)
            StaffMembers.Add(member)
            IsEditMode = False
            StatusMessage = "Staff member added."
        End Sub

        Private Sub DeleteStaff(member As StaffMember)
            If member Is Nothing Then Return
            Dim confirm = System.Windows.MessageBox.Show(
                $"Remove {member.Name} from staff?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

            _store.Staff.Remove(member)
            StaffMembers.Remove(member)
            StatusMessage = $"{member.Name} removed."
        End Sub
    End Class
End Namespace
