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
        Private _statusMessage As String = String.Empty

        Public Sub New()
            StaffList = New ObservableCollection(Of StaffMember)(_store.Staff.Where(Function(s) s.IsActive))
            Appointments = New ObservableCollection(Of AppointmentItem)()
            ServiceNames = New ObservableCollection(Of String)(_store.Services.Select(Function(s) s.Name))

            SelectedStaff = StaffList.FirstOrDefault()
            PrevDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(-1))
            NextDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(1))
            BookCommand = New RelayCommand(AddressOf BeginBook)
            SaveAppointmentCommand = New RelayCommand(AddressOf SaveAppointment)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            ConvertToTransactionCommand = New RelayCommand(Of AppointmentItem)(AddressOf ConvertToTransaction)

            LoadAppointments()
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
        Public Property SaveAppointmentCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property ConvertToTransactionCommand As RelayCommand(Of AppointmentItem)

        Public Sub LoadAppointments()
            Dim staffName = If(SelectedStaff?.Name, String.Empty)
            Appointments = New ObservableCollection(Of AppointmentItem)(
                _store.Appointments.Where(Function(a) a.StartTime.Date = SelectedDate.Date AndAlso a.StaffName = staffName).
                OrderBy(Function(a) a.StartTime))
            OnPropertyChanged(NameOf(Appointments))
        End Sub

        Private Sub BeginBook()
            IsEditMode = True
            EditCustomer = String.Empty
            EditService = ServiceNames.FirstOrDefault()
            EditTime = "09:00"
            EditDuration = 60
        End Sub

        Private Sub SaveAppointment()
            Dim timeParts = EditTime.Split(":"c)
            Dim hour = If(timeParts.Length > 0, Integer.Parse(timeParts(0)), 9)
            Dim minute = If(timeParts.Length > 1, Integer.Parse(timeParts(1)), 0)
            Dim appt As New AppointmentItem With {
                .AppointmentId = _store.Appointments.Count + 1,
                .CustomerName = EditCustomer.Trim(),
                .StaffName = SelectedStaff.Name,
                .ServiceName = EditService,
                .StartTime = SelectedDate.Date.AddHours(hour).AddMinutes(minute),
                .DurationMinutes = EditDuration
            }
            _store.Appointments.Add(appt)
            IsEditMode = False
            StatusMessage = "Appointment booked."
            LoadAppointments()
        End Sub

        Private Sub ConvertToTransaction(appt As AppointmentItem)
            StatusMessage = $"Appointment for {appt.CustomerName} — {appt.ServiceName} ready to convert at Cashier."
        End Sub
    End Class
End Namespace
