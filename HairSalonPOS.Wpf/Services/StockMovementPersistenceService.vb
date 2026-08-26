Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class StockMovementPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of StockMovementPersistenceService)(Function() New StockMovementPersistenceService())
        Private ReadOnly _movementsPath As String
        Private Shared ReadOnly SerializerOptions As New JsonSerializerOptions With {.WriteIndented = True}

        Public Shared ReadOnly Property Instance As StockMovementPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _movementsPath = Path.Combine(folder, "stock-movements.json")
        End Sub

        Public Function Load() As List(Of StockMovement)
            If Not File.Exists(_movementsPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of StockMovement))(File.ReadAllText(_movementsPath))
                If loaded Is Nothing Then Return Nothing
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(movements As IEnumerable(Of StockMovement))
            File.WriteAllText(_movementsPath, JsonSerializer.Serialize(movements.ToList(), SerializerOptions))
        End Sub
    End Class
End Namespace
