Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class UserPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of UserPersistenceService)(Function() New UserPersistenceService())
        Private ReadOnly _usersPath As String

        Public Shared ReadOnly Property Instance As UserPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _usersPath = Path.Combine(folder, "users.json")
        End Sub

        Public Function LoadUsers() As List(Of UserAccount)
            If Not File.Exists(_usersPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of UserAccount))(File.ReadAllText(_usersPath))
                If loaded Is Nothing OrElse loaded.Count = 0 Then Return Nothing
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub SaveUsers(users As IEnumerable(Of UserAccount))
            File.WriteAllText(_usersPath, JsonSerializer.Serialize(users.ToList(), New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
