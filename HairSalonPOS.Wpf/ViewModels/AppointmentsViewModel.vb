Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class AppointmentsViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private _selectedDate As Date = Date.Today
        Private _selectedStaff As StaffMember
        Private _editCustomer As String = String.Empty
        Private _editService As String = String.Empty
        Private _editTime As String = "09:00"
        Private _editDuration As Integer = 60
        Private _isEditMode As Boolean
        Private _isAdding As Boolean = True
        Private _editingAppointmentId As Integer
        Private _statusMessage As String = String.Empty

        Public Sub New()
            StaffList = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            Appointments = New ObservableCollection(Of AppointmentItem)()
            RefreshServiceNames()

            SelectedStaff = StaffList.FirstOrDefault()
            PrevDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(-1))
            NextDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(1))
            BookCommand = New RelayCommand(AddressOf BeginBook)
            EditAppointmentCommand = New RelayCommand(Of AppointmentItem)(AddressOf BeginEdit)
            SaveAppointmentCommand = New RelayCommand(AddressOf SaveAppointment)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            DeleteAppointmentCommand = New RelayCommand(Of AppointmentItem)(AddressOf DeleteAppointment)
            ConvertToTransactionCommand = New RelayCommand(Of AppointmentItem)(AddressOf ConvertToTransaction)

            AddHandler _store.StaffChanged, Sub() RefreshStaffList()
            LoadAppointments()
        End Sub

        Public Sub RefreshStaffList()
            Dim selectedId = If(SelectedStaff?.StaffId, 0)
            StaffList = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            OnPropertyChanged(NameOf(StaffList))
            SelectedStaff = StaffList.FirstOrDefault(Function(s) s.StaffId = selectedId)
            If SelectedStaff Is Nothing Then SelectedStaff = StaffList.FirstOrDefault()
        End Sub

        Public Property StaffList As ObservableCollection(Of StaffMember)
        Public Property Appointments As ObservableCollection(Of AppointmentItem)
        Public Property ServiceNames As ObservableCollection(Of String)

        Public Property SelectedDate As Date
            Get
                Return _selectedDate
            End Get
            Set(value As Date)
                SetProperty(_selectedDate, value)
                LoadAppointments()
                OnPropertyChanged(NameOf(DateLabel))
            End Set
        End Property

        Public ReadOnly Property DateLabel As String
            Get
                Return If(SelectedDate.Date = Date.Today, "Today — ", "") & SelectedDate.ToString("MMMM d, yyyy")
            End Get
        End Property

        Public Property SelectedStaff As StaffMember
            Get
                Return _selectedStaff
            End Get
            Set(value As StaffMember)
                SetProperty(_selectedStaff, value)
                LoadAppointments()
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
                Return If(_isAdding, "Book appointment", "Edit appointment")
            End Get
        End Property

        Public Property EditCustomer As String
            Get
                Return _editCustomer
            End Get
            Set(value As String)
                SetProperty(_editCustomer, value)
            End Set
        End Property

        Public Property EditService As String
            Get
                Return _editService
            End Get
            Set(value As String)
                SetProperty(_editService, value)
            End Set
        End Property

        Public Property EditTime As String
            Get
                Return _editTime
            End Get
            Set(value As String)
                SetProperty(_editTime, value)
            End Set
        End Property

        Public Property EditDuration As Integer
            Get
                Return _editDuration
            End Get
            Set(value As Integer)
                SetProperty(_editDuration, value)
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

        Public Property PrevDayCommand As RelayCommand
        Public Property NextDayCommand As RelayCommand
        Public Property BookCommand As RelayCommand
        Public Property EditAppointmentCommand As RelayCommand(Of AppointmentItem)
        Public Property SaveAppointmentCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property DeleteAppointmentCommand As RelayCommand(Of AppointmentItem)
        Public Property ConvertToTransactionCommand As RelayCommand(Of AppointmentItem)

        Private Sub RefreshServiceNames()
            Dim names = _store.Services.Select(Function(s) s.Name).Distinct().ToList()
            If names.Count = 0 Then names.Add("Custom service")
            ServiceNames = New ObservableCollection(Of String)(names)
            OnPropertyChanged(NameOf(ServiceNames))
        End Sub

        Public Sub LoadAppointments()
            Dim staffName = If(SelectedStaff?.Name, String.Empty)
            Appointments = New ObservableCollection(Of AppointmentItem)(
                _store.Appointments.Where(Function(a) a.StartTime.Date = SelectedDate.Date AndAlso a.StaffName = staffName).
                OrderBy(Function(a) a.StartTime))
            OnPropertyChanged(NameOf(Appointments))
        End Sub

        Private Sub BeginBook()
            RefreshServiceNames()
            _isAdding = True
            _editingAppointmentId = 0
            EditCustomer = String.Empty
            EditService = ServiceNames.FirstOrDefault()
            EditTime = "09:00"
            EditDuration = 60
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(appt As AppointmentItem)
            If appt Is Nothing Then Return
            RefreshServiceNames()
            If Not ServiceNames.Contains(appt.ServiceName) Then ServiceNames.Add(appt.ServiceName)
            _isAdding = False
            _editingAppointmentId = appt.AppointmentId
            EditCustomer = appt.CustomerName
            EditService = appt.ServiceName
            EditTime = appt.StartTime.ToString("HH:mm")
            EditDuration = appt.DurationMinutes
            OnPropertyChanged(NameOf(FormTitle))
            IsEditMode = True
        End Sub

        Private Sub SaveAppointment()
            If SelectedStaff Is Nothing Then
                StatusMessage = "Select a stylist first."
                Return
            End If
            Dim timeParts = EditTime.Split(":"c)
            Dim hour = If(timeParts.Length > 0, Integer.Parse(timeParts(0)), 9)
            Dim minute = If(timeParts.Length > 1, Integer.Parse(timeParts(1)), 0)
            Dim startTime = SelectedDate.Date.AddHours(hour).AddMinutes(minute)

            If _isAdding Then
                Dim appt As New AppointmentItem With {
                    .AppointmentId = If(_store.Appointments.Count = 0, 1, _store.Appointments.Max(Function(a) a.AppointmentId) + 1),
                    .CustomerName = EditCustomer.Trim(),
                    .StaffName = SelectedStaff.Name,
                    .ServiceName = EditService,
                    .StartTime = startTime,
                    .DurationMinutes = EditDuration
                }
                _store.Appointments.Add(appt)
                StatusMessage = "Appointment booked."
            Else
                Dim existing = _store.Appointments.FirstOrDefault(Function(a) a.AppointmentId = _editingAppointmentId)
                If existing Is Nothing Then
                    StatusMessage = "Appointment not found."
                    Return
                End If
                existing.CustomerName = EditCustomer.Trim()
                existing.StaffName = SelectedStaff.Name
                existing.ServiceName = EditService
                existing.StartTime = startTime
                existing.DurationMinutes = EditDuration
                StatusMessage = "Appointment updated."
            End If

            IsEditMode = False
            LoadAppointments()
        End Sub

        Private Sub DeleteAppointment(appt As AppointmentItem)
            If appt Is Nothing Then Return
            Dim confirm = System.Windows.MessageBox.Show(
                $"Delete appointment for {appt.CustomerName}?",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            If confirm <> System.Windows.MessageBoxResult.Yes Then Return

            _store.Appointments.Remove(appt)
            StatusMessage = "Appointment deleted."
            LoadAppointments()
        End Sub

        Private Sub ConvertToTransaction(appt As AppointmentItem)
            StatusMessage = $"Appointment for {appt.CustomerName} — {appt.ServiceName} ready to convert at Cashier."
        End Sub
    End Class
End Namespace
