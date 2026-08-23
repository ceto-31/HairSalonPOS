Imports System.Collections.ObjectModel
Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class AppointmentsViewModel
        Inherits ViewModelBase

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _openAtPointOfSale As Action(Of AppointmentItem)
        Private _selectedDate As Date = Date.Today
        Private _editFirstName As String = String.Empty
        Private _editLastName As String = String.Empty
        Private _editContactNumber As String = String.Empty
        Private _editEmail As String = String.Empty
        Private _editAppointmentDate As Date = Date.Today
        Private _editService As String = String.Empty
        Private _editHour As Integer = 9
        Private _editMinute As Integer = 0
        Private _editDuration As Integer = 60
        Private _selectedBusinessHour As TimeSpan?
        Private _isEditMode As Boolean
        Private _isViewMode As Boolean
        Private _viewAppointment As AppointmentItem
        Private _isAdding As Boolean = True
        Private _editingAppointmentId As Integer
        Private _statusMessage As String = String.Empty
        Private _isDayLoading As Boolean
        Private _selectedDayAppointmentCount As Integer
        Private ReadOnly _dayLoadingTimer As DispatcherTimer

        Public Sub New(openAtPointOfSale As Action(Of AppointmentItem))
            _openAtPointOfSale = openAtPointOfSale
            Appointments = New ObservableCollection(Of AppointmentItem)()
            AvailableBusinessHours = New ObservableCollection(Of TimeSpan)()
            HourOptions = New ObservableCollection(Of Integer)()
            MinuteOptions = New ObservableCollection(Of Integer) From {0, 15, 30, 45}
            RefreshServiceNames()

            PrevDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(-1))
            NextDayCommand = New RelayCommand(Sub() SelectedDate = SelectedDate.AddDays(1))
            BookCommand = New RelayCommand(AddressOf BeginBook)
            EditAppointmentCommand = New RelayCommand(Of AppointmentItem)(AddressOf BeginEdit)
            SaveAppointmentCommand = New RelayCommand(AddressOf SaveAppointment)
            UpdateAppointmentCommand = New RelayCommand(AddressOf UpdateAppointment)
            CancelEditCommand = New RelayCommand(Sub() IsEditMode = False)
            ViewAppointmentCommand = New RelayCommand(Of AppointmentItem)(AddressOf BeginView)
            CancelViewCommand = New RelayCommand(Sub() IsViewMode = False)
            DeleteAppointmentCommand = New RelayCommand(Of AppointmentItem)(AddressOf DeleteAppointment)
            ConvertToTransactionCommand = New RelayCommand(Of AppointmentItem)(AddressOf ConvertToTransaction)
            MarkDoneCommand = New RelayCommand(Of AppointmentItem)(AddressOf MarkDone, AddressOf CanMarkDone)

            _dayLoadingTimer = New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(150)}
            AddHandler _dayLoadingTimer.Tick, AddressOf OnDayLoadingTimerTick

            AddHandler _store.AppointmentsChanged, Sub() LoadAppointments()
            LoadAppointments()
        End Sub

        Public Property Appointments As ObservableCollection(Of AppointmentItem)
        Public Property AvailableBusinessHours As ObservableCollection(Of TimeSpan)
        Public Property ServiceNames As ObservableCollection(Of String)
        Public Property HourOptions As ObservableCollection(Of Integer)
        Public Property MinuteOptions As ObservableCollection(Of Integer)

        Public Property SelectedDate As Date
            Get
                Return _selectedDate
            End Get
            Set(value As Date)
                If SetProperty(_selectedDate, value) Then
                    BeginDayLoading()
                    LoadAppointments()
                    OnPropertyChanged(NameOf(DateLabel))
                    OnPropertyChanged(NameOf(CalendarLinkLabel))
                    OnPropertyChanged(NameOf(SelectedDayBusinessHoursLabel))
                End If
            End Set
        End Property

        Public Property IsDayLoading As Boolean
            Get
                Return _isDayLoading
            End Get
            Private Set(value As Boolean)
                SetProperty(_isDayLoading, value)
            End Set
        End Property

        Public Property SelectedDayAppointmentCount As Integer
            Get
                Return _selectedDayAppointmentCount
            End Get
            Private Set(value As Integer)
                SetProperty(_selectedDayAppointmentCount, value)
            End Set
        End Property

        Public ReadOnly Property SelectedDaySummaryLabel As String
            Get
                Select Case SelectedDayAppointmentCount
                    Case 0
                        Return "No appointments"
                    Case 1
                        Return "1 appointment"
                    Case Else
                        Return $"{SelectedDayAppointmentCount} appointments"
                End Select
            End Get
        End Property

        Public ReadOnly Property HasAppointmentsForSelectedDay As Boolean
            Get
                Return SelectedDayAppointmentCount > 0
            End Get
        End Property

        Public ReadOnly Property CalendarLinkLabel As String
            Get
                Return $"Showing {If(SelectedDate.Date = Date.Today, "today", SelectedDate.ToString("MMMM d, yyyy"))}"
            End Get
        End Property

        Public ReadOnly Property DateLabel As String
            Get
                Return If(SelectedDate.Date = Date.Today, "Today — ", "") & SelectedDate.ToString("MMMM d, yyyy")
            End Get
        End Property

        Public ReadOnly Property SelectedDayBusinessHoursLabel As String
            Get
                Dim day = If(IsEditMode, EditAppointmentDate.Date, SelectedDate.Date)
                Dim hours = BusinessHoursService.GetHours(day)
                Dim openLabel = Date.Today.Add(hours.Open).ToString("h:mm tt")
                Dim closeLabel = Date.Today.Add(hours.Close).ToString("h:mm tt")
                Dim dayType = If(BusinessHoursService.IsWeekend(day), "Weekend", "Weekday")
                Return $"{dayType} hours: {openLabel} – {closeLabel}"
            End Get
        End Property

        Public Property SelectedBusinessHour As TimeSpan?
            Get
                Return _selectedBusinessHour
            End Get
            Set(value As TimeSpan?)
                If SetProperty(_selectedBusinessHour, value) AndAlso value.HasValue Then
                    EditHour = value.Value.Hours
                    EditMinute = value.Value.Minutes
                    EnsureMinuteOption(EditMinute)
                End If
            End Set
        End Property

        Public Property IsEditMode As Boolean
            Get
                Return _isEditMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditMode, value) Then
                    OnPropertyChanged(NameOf(IsListMode))
                    OnPropertyChanged(NameOf(IsAddingAppointment))
                    OnPropertyChanged(NameOf(SelectedDayBusinessHoursLabel))
                    If value Then
                        RefreshAvailableBusinessHours(EditAppointmentDate)
                    Else
                        AvailableBusinessHours.Clear()
                        SelectedBusinessHour = Nothing
                    End If
                End If
            End Set
        End Property

        Public Property IsViewMode As Boolean
            Get
                Return _isViewMode
            End Get
            Set(value As Boolean)
                If SetProperty(_isViewMode, value) Then
                    OnPropertyChanged(NameOf(IsListMode))
                End If
            End Set
        End Property

        Public ReadOnly Property IsListMode As Boolean
            Get
                Return Not IsEditMode AndAlso Not IsViewMode
            End Get
        End Property

        Public ReadOnly Property IsAddingAppointment As Boolean
            Get
                Return IsEditMode AndAlso _isAdding
            End Get
        End Property

        Public Property ViewAppointment As AppointmentItem
            Get
                Return _viewAppointment
            End Get
            Private Set(value As AppointmentItem)
                SetProperty(_viewAppointment, value)
                NotifyViewLabels()
            End Set
        End Property

        Public ReadOnly Property ViewEndTimeLabel As String
            Get
                If ViewAppointment Is Nothing Then Return String.Empty
                Return ViewAppointment.EndTime.ToString("h:mm tt")
            End Get
        End Property

        Public ReadOnly Property ViewDateLabel As String
            Get
                If ViewAppointment Is Nothing Then Return String.Empty
                Return ViewAppointment.StartTime.ToString("dddd, MMMM d, yyyy")
            End Get
        End Property

        Public ReadOnly Property ViewDurationLabel As String
            Get
                If ViewAppointment Is Nothing Then Return String.Empty
                Return $"{ViewAppointment.DurationMinutes} minutes"
            End Get
        End Property

        Public ReadOnly Property FormTitle As String
            Get
                Return If(_isAdding, "Book appointment", "Edit appointment")
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

        Public ReadOnly Property DatesWithAppointments As IEnumerable(Of Date)
            Get
                Return _store.Appointments.Select(Function(a) a.StartTime.Date).Distinct().OrderBy(Function(d) d)
            End Get
        End Property

        Public ReadOnly Property ViewContactLabel As String
            Get
                If ViewAppointment Is Nothing OrElse String.IsNullOrWhiteSpace(ViewAppointment.ContactNumber) Then Return "—"
                Return ViewAppointment.ContactNumber
            End Get
        End Property

        Public ReadOnly Property ViewEmailLabel As String
            Get
                If ViewAppointment Is Nothing OrElse String.IsNullOrWhiteSpace(ViewAppointment.Email) Then Return "—"
                Return ViewAppointment.Email
            End Get
        End Property

        Public ReadOnly Property ViewStatusLabel As String
            Get
                If ViewAppointment Is Nothing Then Return String.Empty
                Return ViewAppointment.DisplayStatusLabel
            End Get
        End Property

        Public Property EditAppointmentDate As Date
            Get
                Return _editAppointmentDate
            End Get
            Set(value As Date)
                If SetProperty(_editAppointmentDate, value) Then
                    RefreshAvailableBusinessHours(value)
                    OnPropertyChanged(NameOf(SelectedDayBusinessHoursLabel))
                End If
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

        Public Property EditHour As Integer
            Get
                Return _editHour
            End Get
            Set(value As Integer)
                If SetProperty(_editHour, value) Then
                    RefreshMinuteOptions()
                    SyncSelectedBusinessHourFromDropdowns()
                End If
            End Set
        End Property

        Public Property EditMinute As Integer
            Get
                Return _editMinute
            End Get
            Set(value As Integer)
                If SetProperty(_editMinute, value) Then
                    SyncSelectedBusinessHourFromDropdowns()
                End If
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
        Public Property UpdateAppointmentCommand As RelayCommand
        Public Property CancelEditCommand As RelayCommand
        Public Property ViewAppointmentCommand As RelayCommand(Of AppointmentItem)
        Public Property CancelViewCommand As RelayCommand
        Public Property DeleteAppointmentCommand As RelayCommand(Of AppointmentItem)
        Public Property ConvertToTransactionCommand As RelayCommand(Of AppointmentItem)
        Public Property MarkDoneCommand As RelayCommand(Of AppointmentItem)

        Private Sub RefreshServiceNames()
            Dim names = _store.Services.Select(Function(s) s.Name).Distinct().ToList()
            If names.Count = 0 Then names.Add("Custom service")
            ServiceNames = New ObservableCollection(Of String)(names)
            OnPropertyChanged(NameOf(ServiceNames))
        End Sub

        Private Sub RefreshAvailableBusinessHours(day As Date)
            Dim preferred = If(_selectedBusinessHour.HasValue, _selectedBusinessHour.Value, New TimeSpan(EditHour, EditMinute, 0))

            AvailableBusinessHours.Clear()
            For Each slot In BusinessHoursService.GetAvailableTimeSlots(day)
                AvailableBusinessHours.Add(slot)
            Next

            HourOptions.Clear()
            For Each hourValue In BusinessHoursService.GetHourOptions(day)
                HourOptions.Add(hourValue)
            Next

            If AvailableBusinessHours.Contains(preferred) Then
                _editHour = preferred.Hours
                _editMinute = preferred.Minutes
            ElseIf AvailableBusinessHours.Count > 0 Then
                preferred = AvailableBusinessHours(0)
                _editHour = preferred.Hours
                _editMinute = preferred.Minutes
            End If

            OnPropertyChanged(NameOf(EditHour))
            RefreshMinuteOptions()
            OnPropertyChanged(NameOf(EditMinute))
            SyncSelectedBusinessHourFromDropdowns()
            OnPropertyChanged(NameOf(SelectedDayBusinessHoursLabel))
        End Sub

        Private Sub RefreshMinuteOptions()
            Dim minutes = BusinessHoursService.GetMinuteOptions(EditAppointmentDate, EditHour).ToList()
            MinuteOptions.Clear()
            For Each minuteValue In minutes
                MinuteOptions.Add(minuteValue)
            Next

            If MinuteOptions.Count = 0 Then
                Return
            End If

            If Not MinuteOptions.Contains(EditMinute) Then
                _editMinute = MinuteOptions(0)
                OnPropertyChanged(NameOf(EditMinute))
            End If
        End Sub

        Private Sub SyncSelectedBusinessHourFromDropdowns()
            Dim slot = New TimeSpan(EditHour, EditMinute, 0)
            If AvailableBusinessHours.Contains(slot) Then
                _selectedBusinessHour = slot
            Else
                _selectedBusinessHour = Nothing
            End If
            OnPropertyChanged(NameOf(SelectedBusinessHour))
        End Sub

        Public Sub LoadAppointments()
            If _store.RefreshAppointmentStatuses() Then
                _store.PersistAppointments()
            End If
            Appointments = New ObservableCollection(Of AppointmentItem)(
                _store.Appointments.Where(Function(a) a.StartTime.Date = SelectedDate.Date).
                OrderBy(Function(a) a.StartTime))
            SelectedDayAppointmentCount = Appointments.Count
            OnPropertyChanged(NameOf(Appointments))
            OnPropertyChanged(NameOf(DatesWithAppointments))
            OnPropertyChanged(NameOf(SelectedDaySummaryLabel))
            OnPropertyChanged(NameOf(HasAppointmentsForSelectedDay))
        End Sub

        Private Sub BeginDayLoading()
            IsDayLoading = True
            _dayLoadingTimer.Stop()
            _dayLoadingTimer.Start()
        End Sub

        Private Sub OnDayLoadingTimerTick(sender As Object, e As EventArgs)
            _dayLoadingTimer.Stop()
            IsDayLoading = False
        End Sub

        Public Sub StartNewBooking()
            SelectedDate = Date.Today
            BeginBook()
        End Sub

        Private Sub BeginBook()
            RefreshServiceNames()
            IsViewMode = False
            _isAdding = True
            _editingAppointmentId = 0
            EditFirstName = String.Empty
            EditLastName = String.Empty
            EditContactNumber = String.Empty
            EditEmail = String.Empty
            EditAppointmentDate = SelectedDate
            EditService = ServiceNames.FirstOrDefault()
            Dim open = BusinessHoursService.GetHours(EditAppointmentDate).Open
            EditHour = open.Hours
            EditMinute = open.Minutes
            EnsureMinuteOption(EditMinute)
            RefreshAvailableBusinessHours(EditAppointmentDate)
            EditDuration = 60
            StatusMessage = String.Empty
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(IsAddingAppointment))
            IsEditMode = True
        End Sub

        Private Sub BeginEdit(appt As AppointmentItem)
            If appt Is Nothing Then Return
            If Not appt.IsScheduled Then
                StatusMessage = "Only scheduled appointments can be edited."
                Return
            End If
            IsViewMode = False
            RefreshServiceNames()
            If Not ServiceNames.Contains(appt.ServiceName) Then ServiceNames.Add(appt.ServiceName)
            _isAdding = False
            _editingAppointmentId = appt.AppointmentId
            Dim parts = SplitName(appt.CustomerName)
            EditFirstName = parts.Item1
            EditLastName = parts.Item2
            EditContactNumber = appt.ContactNumber
            EditEmail = appt.Email
            EditAppointmentDate = appt.StartTime.Date
            EditService = appt.ServiceName
            EnsureMinuteOption(appt.StartTime.Minute)
            EditHour = appt.StartTime.Hour
            EditMinute = appt.StartTime.Minute
            RefreshAvailableBusinessHours(EditAppointmentDate)
            EditDuration = appt.DurationMinutes
            StatusMessage = String.Empty
            OnPropertyChanged(NameOf(FormTitle))
            OnPropertyChanged(NameOf(IsAddingAppointment))
            IsEditMode = True
        End Sub

        Private Sub BeginView(appt As AppointmentItem)
            If appt Is Nothing Then Return
            IsEditMode = False
            ViewAppointment = appt
            IsViewMode = True
        End Sub

        Private Sub SaveAppointment()
            If Not _isAdding Then
                UpdateAppointment()
                Return
            End If

            Dim draft = TryBuildDraftAppointment()
            If draft Is Nothing Then Return

            Dim appt As New AppointmentItem With {
                .AppointmentId = If(_store.Appointments.Count = 0, 1, _store.Appointments.Max(Function(a) a.AppointmentId) + 1),
                .CustomerName = draft.CustomerName,
                .StaffName = String.Empty,
                .ServiceName = draft.ServiceName,
                .StartTime = draft.StartTime,
                .DurationMinutes = draft.DurationMinutes,
                .Status = AppointmentStatuses.Scheduled,
                .ContactNumber = draft.ContactNumber,
                .Email = draft.Email
            }
            _store.Appointments.Add(appt)
            _store.RaiseAppointmentsChanged()
            StatusMessage = "Appointment booked."
            FinishEdit(draft.StartTime.Date)
        End Sub

        Private Sub UpdateAppointment()
            If _isAdding OrElse _editingAppointmentId <= 0 Then
                StatusMessage = "No appointment selected for update."
                AppDialogService.ShowError(StatusMessage, "Update failed")
                Return
            End If

            Dim draft = TryBuildDraftAppointment()
            If draft Is Nothing Then Return

            Dim existing = _store.Appointments.FirstOrDefault(Function(a) a.AppointmentId = _editingAppointmentId)
            If existing Is Nothing Then
                StatusMessage = "Appointment not found."
                AppDialogService.ShowError(StatusMessage, "Update failed")
                Return
            End If

            existing.CustomerName = draft.CustomerName
            existing.ServiceName = draft.ServiceName
            existing.StartTime = draft.StartTime
            existing.DurationMinutes = draft.DurationMinutes
            existing.ContactNumber = draft.ContactNumber
            existing.Email = draft.Email

            If ViewAppointment IsNot Nothing AndAlso ViewAppointment.AppointmentId = existing.AppointmentId Then
                NotifyViewLabels()
            End If

            _store.RaiseAppointmentsChanged()
            StatusMessage = "Appointment updated."
            FinishEdit(draft.StartTime.Date)
        End Sub

        Private Function TryBuildDraftAppointment() As AppointmentItem
            If String.IsNullOrWhiteSpace(EditFirstName) Then
                Return FailValidation("First name is required.")
            End If
            If String.IsNullOrWhiteSpace(EditLastName) Then
                Return FailValidation("Last name is required.")
            End If
            If String.IsNullOrWhiteSpace(EditContactNumber) Then
                Return FailValidation("Contact number is required.")
            End If
            If Not String.IsNullOrWhiteSpace(EditEmail) AndAlso (Not EditEmail.Contains("@"c) OrElse Not EditEmail.Contains("."c)) Then
                Return FailValidation("Enter a valid email address or leave it blank.")
            End If
            If String.IsNullOrWhiteSpace(EditService) Then
                Return FailValidation("Service is required.")
            End If
            If AvailableBusinessHours.Count = 0 Then
                Return FailValidation("No available time slots for the selected date. Choose a later day or time.", "No available slots")
            End If
            If Not HourOptions.Contains(EditHour) OrElse Not MinuteOptions.Contains(EditMinute) Then
                Return FailValidation("Please select a valid start time within business hours.")
            End If
            If EditDuration <= 0 Then
                Return FailValidation("Duration must be greater than zero.")
            End If

            Dim customerName = $"{EditFirstName.Trim()} {EditLastName.Trim()}"
            Dim startTime = EditAppointmentDate.Date.AddHours(EditHour).AddMinutes(EditMinute)
            Dim hoursError = BusinessHoursService.ValidateAppointment(startTime, EditDuration)
            If Not String.IsNullOrEmpty(hoursError) Then
                Dim title = If(hoursError.IndexOf("past", StringComparison.OrdinalIgnoreCase) >= 0,
                               "Past time not allowed",
                               "Outside business hours")
                Return FailValidation(hoursError, title)
            End If

            Return New AppointmentItem With {
                .CustomerName = customerName,
                .ServiceName = EditService,
                .StartTime = startTime,
                .DurationMinutes = EditDuration,
                .ContactNumber = EditContactNumber.Trim(),
                .Email = EditEmail.Trim()
            }
        End Function

        Private Function FailValidation(message As String, Optional title As String = "Cannot save appointment") As AppointmentItem
            StatusMessage = message
            AppDialogService.ShowError(message, title)
            Return Nothing
        End Function

        Private Sub FinishEdit(appointmentDate As Date)
            SelectedDate = appointmentDate
            IsEditMode = False
            LoadAppointments()
        End Sub

        Private Sub NotifyViewLabels()
            OnPropertyChanged(NameOf(ViewEndTimeLabel))
            OnPropertyChanged(NameOf(ViewDateLabel))
            OnPropertyChanged(NameOf(ViewDurationLabel))
            OnPropertyChanged(NameOf(ViewContactLabel))
            OnPropertyChanged(NameOf(ViewEmailLabel))
            OnPropertyChanged(NameOf(ViewStatusLabel))
        End Sub

        Private Sub EnsureMinuteOption(minute As Integer)
            If Not MinuteOptions.Contains(minute) Then
                MinuteOptions.Add(minute)
                MinuteOptions = New ObservableCollection(Of Integer)(MinuteOptions.OrderBy(Function(m) m))
                OnPropertyChanged(NameOf(MinuteOptions))
            End If
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

        Private Sub DeleteAppointment(appt As AppointmentItem)
            If appt Is Nothing Then Return
            If Not AppDialogService.ConfirmDelete($"appointment for {appt.CustomerName}") Then Return

            _store.Appointments.Remove(appt)
            _store.RaiseAppointmentsChanged()
            StatusMessage = "Appointment deleted."
            LoadAppointments()
        End Sub

        Private Sub ConvertToTransaction(appt As AppointmentItem)
            If appt Is Nothing OrElse Not appt.IsScheduled Then Return
            IsViewMode = False
            _openAtPointOfSale?.Invoke(appt)
        End Sub

        Private Function CanMarkDone(appt As AppointmentItem) As Boolean
            Return appt IsNot Nothing AndAlso appt.IsScheduled
        End Function

        Private Sub MarkDone(appt As AppointmentItem)
            If appt Is Nothing OrElse Not appt.IsScheduled Then Return
            _store.MarkAppointmentDone(appt.AppointmentId)
            StatusMessage = $"Marked {appt.CustomerName} as done."
            LoadAppointments()
        End Sub
    End Class
End Namespace
