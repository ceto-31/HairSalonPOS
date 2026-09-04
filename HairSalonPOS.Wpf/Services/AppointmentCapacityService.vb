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

        Public Function GetBookingCount(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            category As String,
            Optional excludeAppointmentId As Integer = 0) As Integer

            If String.IsNullOrWhiteSpace(category) Then Return 0

            Dim targetDate = day.Date
            Dim targetSlot = startTime.TimeOfDay

            Return appointments.Where(Function(a)
                                          If Not a.IsOpen Then Return False
                                          If excludeAppointmentId > 0 AndAlso a.AppointmentId = excludeAppointmentId Then Return False
                                          If a.StartTime.Date <> targetDate Then Return False
                                          If a.StartTime.TimeOfDay <> targetSlot Then Return False
                                          Dim apptCategory = ResolveServiceCategory(services, a.ServiceName)
                                          Return apptCategory.Equals(category, StringComparison.OrdinalIgnoreCase)
                                      End Function).Count()
        End Function

        Public Function IsSlotAvailable(
            appointments As IEnumerable(Of AppointmentItem),
            services As IEnumerable(Of ServiceItem),
            day As Date,
            startTime As DateTime,
            category As String,
            Optional excludeAppointmentId As Integer = 0) As Boolean

            Dim max = GetMaxCapacity(category)
            If max <= 0 Then Return False
            Dim count = GetBookingCount(appointments, services, day, startTime, category, excludeAppointmentId)
            Return count < max
        End Function

        Public Function GetFullyBookedMessage(category As String) As String
            Dim max = GetMaxCapacity(category)
            Return $"This time slot is fully booked for {FormatCategoryDisplay(category)} ({max}/{max})."
        End Function

        Public Function GetDayFullyBookedMessage(category As String, day As Date) As String
            Return $"No available slots — fully booked for {FormatCategoryDisplay(category)} on {day:MMMM d, yyyy}."
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
