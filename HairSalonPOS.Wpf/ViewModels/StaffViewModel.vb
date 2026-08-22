Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class StaffViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _images As CatalogImageService = CatalogImageService.Instance
        Private _editFirstName As String = String.Empty
        Private _editLastName As String = String.Empty
        Private _editRole As String = "Stylist"
        Private _editContactNumber As String = String.Empty
        Private _editEmail As String = String.Empty
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingStaffId As Integer
        Private _statusMessage As String = String.Empty
        Private _showArchived As Boolean
        Private _isHostedInMasterFiles As Boolean
        Private _editImagePath As String = String.Empty
        Private _pendingSourcePath As String
        Private _originalImagePath As String = String.Empty
        Private _imageRemoved As Boolean

        Public Sub New()
            StaffMembers = New ObservableCollection(Of StaffMember)()
            AddStaffCommand = New RelayCommand(AddressOf BeginAdd)
            EditStaffCommand = New RelayCommand(Of StaffMember)(AddressOf BeginEdit)
            SaveStaffCommand = New RelayCommand(AddressOf SaveStaff)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteStaffCommand = New RelayCommand(Of StaffMember)(AddressOf DeleteStaff)
            ArchiveStaffCommand = New RelayCommand(Of StaffMember)(AddressOf ArchiveStaff)
            UnarchiveStaffCommand = New RelayCommand(Of StaffMember)(AddressOf UnarchiveStaff)
            ToggleShowArchivedCommand = New RelayCommand(AddressOf ToggleShowArchived)
            ChooseImageCommand = New RelayCommand(AddressOf ChooseImage)
            RemoveImageCommand = New RelayCommand(AddressOf RemoveImage)
            LoadFromStore()
        End Sub

        Public Sub LoadFromStore()
            RefreshList()
            StatusMessage = String.Empty
        End Sub

        Public Property IsHostedInMasterFiles As Boolean
            Get
                Return _isHostedInMasterFiles
            End Get
            Set(value As Boolean)
                SetProperty(_isHostedInMasterFiles, value)
            End Set
        End Property

        Public Property StaffMembers As ObservableCollection(Of StaffMember)

        Public Property ShowArchived As Boolean
            Get
                Return _showArchived
            End Get
            Set(value As Boolean)
                If SetProperty(_showArchived, value) Then
                    RefreshList()
                    OnPropertyChanged(NameOf(ShowArchivedLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property ShowArchivedLabel As String
            Get
                Return If(ShowArchived, "Hide archived", "Show archived")
            End Get
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
                Return If(_isAdding, "Add staff member", "Edit staff member")
            End Get
        End Property

        Public Property EditFirstName As String
            Get
                Return _editFirstName
            End Get
            Set(value As String)
                If SetProperty(_editFirstName, value) Then
                    OnPropertyChanged(NameOf(EditInitials))
                End If
            End Set
        End Property

        Public Property EditLastName As String
            Get
                Return _editLastName
            End Get
            Set(value As String)
                If SetProperty(_editLastName, value) Then
                    OnPropertyChanged(NameOf(EditInitials))
                End If
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

        Public Property EditContactNumber As String
            Get
                Return _editContactNumber
            End Get
            Set(value As String)
                SetProperty(_editContactNumber, value)
            End Set
        End Property

        Public Property EditEmail As String
            Get
                Return _editEmail
            End Get
            Set(value As String)
                SetProperty(_editEmail, value)
            End Set
        End Property

        Public Property EditImagePath As String
            Get
                Return _editImagePath
            End Get
            Set(value As String)
                If SetProperty(_editImagePath, If(value, String.Empty)) Then
                    OnPropertyChanged(NameOf(HasEditImage))
                    OnPropertyChanged(NameOf(ChooseImageLabel))
                End If
            End Set
        End Property

        Public ReadOnly Property HasEditImage As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(EditImagePath)
            End Get
        End Property

        Public ReadOnly Property ChooseImageLabel As String
            Get
                Return If(HasEditImage, "Change photo", "Choose photo")
            End Get
        End Property

        Public ReadOnly Property EditInitials As String
            Get
                Dim parts = New List(Of String)
                If Not String.IsNullOrWhiteSpace(EditFirstName) Then parts.Add(EditFirstName.Trim())
                If Not String.IsNullOrWhiteSpace(EditLastName) Then parts.Add(EditLastName.Trim())
                If parts.Count = 0 Then Return "?"
                Return String.Join("", parts.Take(2).Select(Function(p) p(0).ToString())).ToUpper()
            End Get
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
        Public Property ArchiveStaffCommand As RelayCommand(Of StaffMember)
        Public Property UnarchiveStaffCommand As RelayCommand(Of StaffMember)
        Public Property ToggleShowArchivedCommand As RelayCommand
        Public Property ChooseImageCommand As RelayCommand
        Public Property RemoveImageCommand As RelayCommand

        Private Sub ToggleShowArchived()
            ShowArchived = Not ShowArchived
        End Sub

        Private Sub RefreshList()
            Dim query = _store.Staff.AsEnumerable()
            If Not ShowArchived Then
                query = query.Where(Function(s) s.IsActive)
            End If
            StaffMembers = New ObservableCollection(Of StaffMember)(query.OrderBy(Function(s) s.Name))
            OnPropertyChanged(NameOf(StaffMembers))
        End Sub

        Private Sub BeginAdd()
            _isAdding = True
            _editingStaffId = 0
            EditFirstName = String.Empty
            EditLastName = String.Empty
            EditRole = "Stylist"
            EditContactNumber = String.Empty
            EditEmail = String.Empty
            StatusMessage = String.Empty
            ResetImageEdit(String.Empty)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(EditInitials))
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
            EditContactNumber = member.ContactNumber
            EditEmail = member.Email
            StatusMessage = String.Empty
            ResetImageEdit(member.ImagePath)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(EditInitials))
            IsEditMode = True
        End Sub

        Private Sub SaveStaff()
            If String.IsNullOrWhiteSpace(EditFirstName) Then
                FailValidation("First name is required.")
                Return
            End If
            If String.IsNullOrWhiteSpace(EditLastName) Then
                FailValidation("Last name is required.")
                Return
            End If
            If String.IsNullOrWhiteSpace(EditContactNumber) Then
                FailValidation("Contact number is required.")
                Return
            End If
            If String.IsNullOrWhiteSpace(EditEmail) Then
                FailValidation("Email is required.")
                Return
            End If

            Dim fullName = $"{EditFirstName.Trim()} {EditLastName.Trim()}"

            If _isAdding Then
                Dim staffId = If(_store.Staff.Count = 0, 1, _store.Staff.Max(Function(s) s.StaffId) + 1)
                Dim imagePath = CommitImage(staffId.ToString())
                If imagePath Is Nothing AndAlso _pendingSourcePath IsNot Nothing Then
                    StatusMessage = "Could not save the photo."
                    Return
                End If
                Dim member As New StaffMember With {
                    .StaffId = staffId,
                    .Name = fullName,
                    .Role = EditRole.Trim(),
                    .ContactNumber = EditContactNumber.Trim(),
                    .Email = EditEmail.Trim(),
                    .IsActive = True,
                    .ImagePath = If(imagePath, String.Empty)
                }
                _store.Staff.Add(member)
                StatusMessage = "Staff member added."
            Else
                Dim existing = _store.Staff.FirstOrDefault(Function(s) s.StaffId = _editingStaffId)
                If existing Is Nothing Then
                    StatusMessage = "Staff member not found."
                    Return
                End If
                Dim imagePath = CommitImage(existing.StaffId.ToString())
                If imagePath Is Nothing AndAlso _pendingSourcePath IsNot Nothing Then
                    StatusMessage = "Could not save the photo."
                    Return
                End If
                existing.Name = fullName
                existing.Role = EditRole.Trim()
                existing.ContactNumber = EditContactNumber.Trim()
                existing.Email = EditEmail.Trim()
                existing.ImagePath = If(imagePath, String.Empty)
                StatusMessage = "Staff member updated."
            End If

            _store.RaiseStaffChanged()
            RefreshList()
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

            _images.DeleteImage(member.ImagePath)
            _store.Staff.Remove(member)
            StatusMessage = $"{member.Name} removed."
            _store.RaiseStaffChanged()
            RefreshList()
        End Sub

        Private Sub ArchiveStaff(member As StaffMember)
            If member Is Nothing OrElse Not member.IsActive Then Return
            member.IsActive = False
            StatusMessage = $"{member.Name} archived."
            _store.RaiseStaffChanged()
            RefreshList()
        End Sub

        Private Sub UnarchiveStaff(member As StaffMember)
            If member Is Nothing OrElse member.IsActive Then Return
            member.IsActive = True
            StatusMessage = $"{member.Name} restored."
            _store.RaiseStaffChanged()
            RefreshList()
        End Sub

        Private Sub FailValidation(message As String)
            StatusMessage = message
            AppDialogService.ShowError(message, "Required fields")
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

        Private Sub ChooseImage()
            Dim picked = _images.PickImageFile()
            If picked Is Nothing Then Return
            _pendingSourcePath = picked
            _imageRemoved = False
            EditImagePath = picked
        End Sub

        Private Sub RemoveImage()
            _pendingSourcePath = Nothing
            _imageRemoved = True
            EditImagePath = String.Empty
        End Sub

        Private Sub ResetImageEdit(existingPath As String)
            _pendingSourcePath = Nothing
            _imageRemoved = False
            _originalImagePath = If(existingPath, String.Empty)
            EditImagePath = _originalImagePath
        End Sub

        Private Function CommitImage(id As String) As String
            If _pendingSourcePath IsNot Nothing Then
                Dim saved = _images.SaveImage(_pendingSourcePath, CatalogImageService.StaffKind, id)
                If saved Is Nothing Then Return Nothing
                If Not String.IsNullOrWhiteSpace(_originalImagePath) AndAlso
                   Not _originalImagePath.Equals(saved, StringComparison.OrdinalIgnoreCase) Then
                    _images.DeleteImage(_originalImagePath)
                End If
                Return saved
            End If

            If _imageRemoved Then
                _images.DeleteImage(_originalImagePath)
                Return String.Empty
            End If

            Return _originalImagePath
        End Function
    End Class
End Namespace
