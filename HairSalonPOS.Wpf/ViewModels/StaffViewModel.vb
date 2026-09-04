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
        Private _editCategory As String = StaffCategories.HairSpecialists
        Private _editCategoryError As String = String.Empty
        Private _editContactNumber As String = String.Empty
        Private _editEmail As String = String.Empty
        Private _editFirstNameError As String = String.Empty
        Private _editLastNameError As String = String.Empty
        Private _editContactNumberError As String = String.Empty
        Private _editEmailError As String = String.Empty
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingStaffId As Integer
        Private _statusMessage As String = String.Empty
        Private _showArchived As Boolean
        Private _searchText As String = String.Empty
        Private _isHostedInMasterFiles As Boolean
        Private _editImagePath As String = String.Empty
        Private _pendingSourcePath As String
        Private _originalImagePath As String = String.Empty
        Private _imageRemoved As Boolean

        Public Sub New()
            StaffMembers = New ObservableCollection(Of StaffMember)()
            CategoryOptions = New ObservableCollection(Of String)(StaffCategories.All)
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
        Public Property CategoryOptions As ObservableCollection(Of String)

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

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then
                    RefreshList()
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
                Return If(_isAdding, "Add staff member", "Edit staff member")
            End Get
        End Property

        Public Property EditFirstName As String
            Get
                Return _editFirstName
            End Get
            Set(value As String)
                If SetProperty(_editFirstName, value) Then
                    EditFirstNameError = String.Empty
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
                    EditLastNameError = String.Empty
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

        Public Property EditCategory As String
            Get
                Return _editCategory
            End Get
            Set(value As String)
                If SetProperty(_editCategory, value) Then
                    EditCategoryError = String.Empty
                End If
            End Set
        End Property

        Public Property EditCategoryError As String
            Get
                Return _editCategoryError
            End Get
            Set(value As String)
                SetProperty(_editCategoryError, value)
            End Set
        End Property

        Public Property EditContactNumber As String
            Get
                Return _editContactNumber
            End Get
            Set(value As String)
                Dim normalized = NormalizeContactDigits(value)
                If SetProperty(_editContactNumber, normalized) Then
                    EditContactNumberError = String.Empty
                End If
            End Set
        End Property

        Public Property EditEmail As String
            Get
                Return _editEmail
            End Get
            Set(value As String)
                If SetProperty(_editEmail, value) Then
                    EditEmailError = String.Empty
                End If
            End Set
        End Property

        Public Property EditFirstNameError As String
            Get
                Return _editFirstNameError
            End Get
            Set(value As String)
                SetProperty(_editFirstNameError, value)
            End Set
        End Property

        Public Property EditLastNameError As String
            Get
                Return _editLastNameError
            End Get
            Set(value As String)
                SetProperty(_editLastNameError, value)
            End Set
        End Property

        Public Property EditContactNumberError As String
            Get
                Return _editContactNumberError
            End Get
            Set(value As String)
                SetProperty(_editContactNumberError, value)
            End Set
        End Property

        Public Property EditEmailError As String
            Get
                Return _editEmailError
            End Get
            Set(value As String)
                SetProperty(_editEmailError, value)
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
            If ShowArchived Then
                query = query.Where(Function(s) Not s.IsActive)
            Else
                query = query.Where(Function(s) s.IsActive)
            End If
            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                query = query.Where(Function(s) s.Name.ToLowerInvariant().Contains(term) OrElse
                    (Not String.IsNullOrWhiteSpace(s.Role) AndAlso s.Role.ToLowerInvariant().Contains(term)) OrElse
                    (Not String.IsNullOrWhiteSpace(s.Category) AndAlso s.Category.ToLowerInvariant().Contains(term)) OrElse
                    (Not String.IsNullOrWhiteSpace(s.ContactNumber) AndAlso s.ContactNumber.ToLowerInvariant().Contains(term)) OrElse
                    (Not String.IsNullOrWhiteSpace(s.Email) AndAlso s.Email.ToLowerInvariant().Contains(term)))
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
            EditCategory = StaffCategories.HairSpecialists
            EditContactNumber = String.Empty
            EditEmail = String.Empty
            ClearFieldErrors()
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
            EditCategory = ResolveEditCategory(member.Category)
            EditContactNumber = member.ContactNumber
            EditEmail = member.Email
            ClearFieldErrors()
            StatusMessage = String.Empty
            ResetImageEdit(member.ImagePath)
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(EditInitials))
            IsEditMode = True
        End Sub

        Private Sub SaveStaff()
            ClearFieldErrors()
            Dim hasErrors = False

            If String.IsNullOrWhiteSpace(EditFirstName) Then
                EditFirstNameError = "First name is required."
                hasErrors = True
            End If
            If String.IsNullOrWhiteSpace(EditLastName) Then
                EditLastNameError = "Last name is required."
                hasErrors = True
            End If
            If String.IsNullOrWhiteSpace(EditContactNumber) Then
                EditContactNumberError = "Contact number is required."
                hasErrors = True
            ElseIf EditContactNumber.Length <> 11 Then
                EditContactNumberError = "Contact number must be exactly 11 digits."
                hasErrors = True
            End If
            If Not String.IsNullOrWhiteSpace(EditEmail) AndAlso (Not EditEmail.Contains("@"c) OrElse Not EditEmail.Contains("."c)) Then
                EditEmailError = "Enter a valid email address or leave it blank."
                hasErrors = True
            End If
            If String.IsNullOrWhiteSpace(EditCategory) OrElse
               Not StaffCategories.All.Any(Function(c) c.Equals(EditCategory, StringComparison.OrdinalIgnoreCase)) Then
                EditCategoryError = "Select a staff category."
                hasErrors = True
            End If

            If hasErrors Then Return

            Dim fullName = $"{EditFirstName.Trim()} {EditLastName.Trim()}"
            Dim email = If(EditEmail, String.Empty).Trim()

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
                    .Category = EditCategory.Trim(),
                    .ContactNumber = EditContactNumber.Trim(),
                    .Email = email,
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
                existing.Category = EditCategory.Trim()
                existing.ContactNumber = EditContactNumber.Trim()
                existing.Email = email
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

        Private Sub ClearFieldErrors()
            EditFirstNameError = String.Empty
            EditLastNameError = String.Empty
            EditCategoryError = String.Empty
            EditContactNumberError = String.Empty
            EditEmailError = String.Empty
        End Sub

        Private Shared Function ResolveEditCategory(category As String) As String
            If String.IsNullOrWhiteSpace(category) Then
                Return StaffCategories.HairSpecialists
            End If

            Dim match = StaffCategories.All.FirstOrDefault(Function(c) c.Equals(category, StringComparison.OrdinalIgnoreCase))
            Return If(match, StaffCategories.HairSpecialists)
        End Function

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

        Private Shared Function NormalizeContactDigits(value As String) As String
            If String.IsNullOrEmpty(value) Then Return String.Empty
            Dim digits = New String(value.Where(Function(c) Char.IsDigit(c)).ToArray())
            Return If(digits.Length <= 11, digits, digits.Substring(0, 11))
        End Function
    End Class
End Namespace
