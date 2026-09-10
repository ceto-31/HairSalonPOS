Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Module AppointmentCapacityService
        Private ReadOnly CapacityByCategory As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
            {"HAIR SERVICES", 5},
            {"NAIL SERVICES", 3},
            {"BODY SERVICES", 7},
            {"EYELASH SERVICES", 7},
            {"EYEBROW SERVICES", 7},
            {"WAXING SERVICES", 7}
        }

        Public Function GetMaxCapacity(categoryName As String) As Integer
            If String.IsNullOrWhiteSpace(categoryName) Then Return 0
            Dim max As Integer
            If CapacityByCategory.TryGetValue(categoryName.Trim(), max) Then Return max
            Return 0
        End Function

        Public Function ResolveServiceCategory(services As IEnumerable(Of ServiceItem), serviceName As String) As String
            If String.IsNullOrWhiteSpace(serviceName) Then Return String.Empty
            Dim service = services.FirstOrDefault(
                Function(s) s.Name.Equals(serviceName.Trim(), StringComparison.OrdinalIgnoreCase))
            If service Is Nothing OrElse String.IsNullOrWhiteSpace(service.Category) Then Return String.Empty
            Return service.Category.Trim()
        End Function

        Public Function ResolveServiceDurationMinutes(services As IEnumerable(Of ServiceItem), serviceName As String) As Integer
            If String.IsNullOrWhiteSpace(serviceName) Then Return 60
            Dim service = services.FirstOrDefault(
                Function(s) s.IsActive AndAlso s.Name.Equals(serviceName.Trim(), StringComparison.OrdinalIgnoreCase))
            If service Is Nothing Then Return 60
            Dim minDuration = service.EffectiveMinDurationMinutes()
            If minDuration > 0 Then Return minDuration
            If service.DurationMinutes > 0 Then Return service.DurationMinutes
            Return 60
        End Function

        Public Function GetAppointmentDurationMinutes(
            appointment As AppointmentItem,
            services As IEnumerable(Of ServiceItem)) As Integer

            If appointment Is Nothing Then Return 60
            If appointment.DurationMinutes > 0 Then Return appointment.DurationMinutes
            Return ResolveServiceDurationMinutes(services, appointment.ServiceName)
        End Function

        Public Function GetAppointmentEndTime(
            appointment As AppointmentItem,
            services As IEnumerable(Of ServiceItem)) As DateTime

            If appointment Is Nothing Then Return DateTime.MinValue
            Return appointment.StartTime.AddMinutes(GetAppointmentDurationMinutes(appointment, services))
        End Function

        Public Function IntervalsOverlap(startA As DateTime, endA As DateTime, startB As DateTime, endB As DateTime) As Boolean
            Return startA < endB AndAlso startB < endA
        End Function

        Public Function GetOverlappingBookingCount(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            durationMinutes As Integer,
            category As String,
            Optional excludeAppointmentId As Integer = 0) As Integer

            If String.IsNullOrWhiteSpace(category) Then Return 0
            If durationMinutes <= 0 Then Return 0

            Dim proposedEnd = startTime.AddMinutes(durationMinutes)
            Dim targetDate = day.Date

            Return appointments.Where(Function(a)
                                          If Not a.IsOpen Then Return False
                                          If excludeAppointmentId > 0 AndAlso a.AppointmentId = excludeAppointmentId Then Return False
                                          If a.StartTime.Date <> targetDate Then Return False
                                          Dim apptCategory = ResolveServiceCategory(services, a.ServiceName)
                                          If Not apptCategory.Equals(category, StringComparison.OrdinalIgnoreCase) Then Return False
                                          Dim apptEnd = GetAppointmentEndTime(a, services)
                                          Return IntervalsOverlap(startTime, proposedEnd, a.StartTime, apptEnd)
                                      End Function).Count()
        End Function

        Public Function HasStaffOverlap(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            durationMinutes As Integer,
            staffName As String,
            Optional excludeAppointmentId As Integer = 0) As Boolean

            If String.IsNullOrWhiteSpace(staffName) Then Return False
            If durationMinutes <= 0 Then Return False

            Dim proposedEnd = startTime.AddMinutes(durationMinutes)
            Dim normalizedStaff = staffName.Trim()

            Return appointments.Any(Function(a)
                                        If Not a.IsOpen Then Return False
                                        If excludeAppointmentId > 0 AndAlso a.AppointmentId = excludeAppointmentId Then Return False
                                        If a.StartTime.Date <> day.Date Then Return False
                                        If String.IsNullOrWhiteSpace(a.StaffName) Then Return False
                                        If Not a.StaffName.Trim().Equals(normalizedStaff, StringComparison.OrdinalIgnoreCase) Then Return False
                                        Dim apptEnd = GetAppointmentEndTime(a, services)
                                        Return IntervalsOverlap(startTime, proposedEnd, a.StartTime, apptEnd)
                                    End Function)
        End Function

        Public Function IsBookingAvailable(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            durationMinutes As Integer,
            category As String,
            staffName As String,
            Optional excludeAppointmentId As Integer = 0) As Boolean

            Return String.IsNullOrEmpty(
                GetBookingConflictMessage(appointments, services, day, startTime, durationMinutes, category, staffName, excludeAppointmentId))
        End Function

        Public Function GetBookingConflictMessage(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            durationMinutes As Integer,
            category As String,
            staffName As String,
            Optional excludeAppointmentId As Integer = 0) As String

            If durationMinutes <= 0 Then Return "Selected service has no valid duration."

            Dim max = GetMaxCapacity(category)
            If max <= 0 Then Return "Selected service category is not configured for booking."

            Dim count = GetOverlappingBookingCount(
                appointments, services, day, startTime, durationMinutes, category, excludeAppointmentId)
            If count >= max Then
                Return GetFullyBookedMessage(category, count, max)
            End If

            Dim staffMessage = GetStaffUnavailableMessage(
                appointments, services, day, startTime, durationMinutes, staffName, excludeAppointmentId)
            If Not String.IsNullOrEmpty(staffMessage) Then Return staffMessage

            Return String.Empty
        End Function

        Public Function GetStaffUnavailableMessage(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            durationMinutes As Integer,
            staffName As String,
            Optional excludeAppointmentId As Integer = 0) As String

            If String.IsNullOrWhiteSpace(staffName) Then Return String.Empty
            If Not HasStaffOverlap(appointments, services, day, startTime, durationMinutes, staffName, excludeAppointmentId) Then
                Return String.Empty
            End If

            Dim endTime = startTime.AddMinutes(durationMinutes)
            Return $"{staffName.Trim()} is already booked from {startTime:h:mm tt} to {endTime:h:mm tt}. Please choose a different time or staff member."
        End Function

        Public Function GetFullyBookedMessage(category As String, count As Integer, max As Integer) As String
            Return $"This time slot is fully booked for {FormatCategoryDisplay(category)} ({Math.Min(count, max)}/{max})."
        End Function

        Public Function GetFullyBookedMessage(category As String) As String
            Dim max = GetMaxCapacity(category)
            Return GetFullyBookedMessage(category, max, max)
        End Function

        Public Function GetDayFullyBookedMessage(category As String, day As Date) As String
            Return $"{FormatCategoryDisplay(category)} has no available time slots on {day:MMMM d, yyyy}."
        End Function

        Public Function FormatCategoryDisplay(category As String) As String
            If String.IsNullOrWhiteSpace(category) Then Return String.Empty
            Dim words = category.Trim().Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            Return String.Join(" ", words.Select(Function(w)
                                                     If w.Length = 0 Then Return w
                                                     Return Char.ToUpperInvariant(w(0)) & w.Substring(1).ToLowerInvariant()
                                                 End Function))
        End Function
    End Module
End Namespace
