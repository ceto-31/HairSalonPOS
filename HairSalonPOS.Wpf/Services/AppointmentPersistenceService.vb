Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class AppointmentPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of AppointmentPersistenceService)(Function() New AppointmentPersistenceService())
        Private ReadOnly _appointmentsPath As String

        Public Shared ReadOnly Property Instance As AppointmentPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _appointmentsPath = Path.Combine(folder, "appointments.json")
        End Sub

        Public Function Load() As List(Of AppointmentItem)
            If Not File.Exists(_appointmentsPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of AppointmentItem))(File.ReadAllText(_appointmentsPath))
                If loaded Is Nothing Then Return Nothing
                For Each appt In loaded
                    If String.IsNullOrWhiteSpace(appt.Status) Then
                        appt.Status = AppointmentStatuses.Scheduled
                    End If
                    If appt.ContactNumber Is Nothing Then appt.ContactNumber = String.Empty
                    If appt.Email Is Nothing Then appt.Email = String.Empty
                Next
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(appointments As IEnumerable(Of AppointmentItem))
            File.WriteAllText(_appointmentsPath, JsonSerializer.Serialize(appointments.ToList(), New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
