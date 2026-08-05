Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class StaffPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of StaffPersistenceService)(Function() New StaffPersistenceService())
        Private ReadOnly _staffPath As String

        Public Shared ReadOnly Property Instance As StaffPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _staffPath = Path.Combine(folder, "staff.json")
        End Sub

        Public Function Load() As List(Of StaffMember)
            If Not File.Exists(_staffPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of StaffMember))(File.ReadAllText(_staffPath))
                If loaded Is Nothing OrElse loaded.Count = 0 Then Return Nothing
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(staff As IEnumerable(Of StaffMember))
            File.WriteAllText(_staffPath, JsonSerializer.Serialize(staff.ToList(), New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
