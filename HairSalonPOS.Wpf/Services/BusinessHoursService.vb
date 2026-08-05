Namespace Services
    Public Module BusinessHoursService
        Public ReadOnly WeekdayOpen As New TimeSpan(8, 30, 0)
        Public ReadOnly WeekdayClose As New TimeSpan(17, 30, 0)
        Public ReadOnly WeekendOpen As New TimeSpan(9, 0, 0)
        Public ReadOnly WeekendClose As New TimeSpan(18, 30, 0)

        Public Const DisplayText As String = "Mon–Fri 8:30am–5:30pm · Sat–Sun 9:00am–6:30pm"

        Public Function IsWeekend(day As Date) As Boolean
            Return day.DayOfWeek = DayOfWeek.Saturday OrElse day.DayOfWeek = DayOfWeek.Sunday
        End Function

        Public Function GetHours(day As Date) As (Open As TimeSpan, Close As TimeSpan)
            If IsWeekend(day) Then
                Return (WeekendOpen, WeekendClose)
            End If
            Return (WeekdayOpen, WeekdayClose)
        End Function

        Public Function FormatHoursMessage(day As Date) As String
            If IsWeekend(day) Then
                Return "Weekend hours are 9:00 AM – 6:30 PM."
            End If
            Return "Weekday hours are 8:30 AM – 5:30 PM."
        End Function

        Public Function IsWithinBusinessHours(startTime As DateTime, endTime As DateTime) As Boolean
            If startTime.Date <> endTime.Date Then Return False

            Dim hours = GetHours(startTime.Date)
            Dim startOfDay = startTime.TimeOfDay
            Dim endOfDay = endTime.TimeOfDay
            Return startOfDay >= hours.Open AndAlso endOfDay <= hours.Close AndAlso endTime > startTime
        End Function

        Public Function ValidateAppointment(startTime As DateTime, durationMinutes As Integer) As String
            If durationMinutes <= 0 Then
                Return "Duration must be greater than zero."
            End If

            If startTime.Date < Date.Today Then
                Return "Cannot book an appointment on a past date."
            End If

            If startTime < DateTime.Now Then
                Return "Cannot book an appointment in the past. Choose the current time or a later slot."
            End If

            Dim endTime = startTime.AddMinutes(durationMinutes)
            If IsWithinBusinessHours(startTime, endTime) Then
                Return String.Empty
            End If

            Return $"Appointment is outside business hours. {FormatHoursMessage(startTime.Date)}"
        End Function

        Public Function GetAvailableTimeSlots(day As Date, Optional intervalMinutes As Integer = 15, Optional excludePastTimes As Boolean = True) As IEnumerable(Of TimeSpan)
            Dim hours = GetHours(day)
            Dim slots As New List(Of TimeSpan)
            Dim current = hours.Open
            Dim now = DateTime.Now
            While current < hours.Close
                Dim slotDateTime = day.Date.Add(current)
                If Not excludePastTimes OrElse day.Date > Date.Today OrElse slotDateTime >= now Then
                    slots.Add(current)
                End If
                current = current.Add(TimeSpan.FromMinutes(intervalMinutes))
            End While
            Return slots
        End Function

        Public Function GetHourOptions(day As Date) As IEnumerable(Of Integer)
            Return GetAvailableTimeSlots(day).Select(Function(t) t.Hours).Distinct().OrderBy(Function(h) h)
        End Function

        Public Function GetMinuteOptions(day As Date, hour As Integer) As IEnumerable(Of Integer)
            Return GetAvailableTimeSlots(day).
                Where(Function(t) t.Hours = hour).
                Select(Function(t) t.Minutes).
                Distinct().
                OrderBy(Function(m) m)
        End Function
    End Module
End Namespace
