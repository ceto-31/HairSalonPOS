Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class StockBatchPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of StockBatchPersistenceService)(Function() New StockBatchPersistenceService())
        Private ReadOnly _batchesPath As String
        Private Shared ReadOnly SerializerOptions As New JsonSerializerOptions With {.WriteIndented = True}

        Public Shared ReadOnly Property Instance As StockBatchPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _batchesPath = Path.Combine(folder, "stock-batches.json")
        End Sub

        Public Function Load() As List(Of StockBatch)
            If Not File.Exists(_batchesPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of StockBatch))(File.ReadAllText(_batchesPath))
                If loaded Is Nothing Then Return Nothing
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(batches As IEnumerable(Of StockBatch))
            File.WriteAllText(_batchesPath, JsonSerializer.Serialize(batches.ToList(), SerializerOptions))
        End Sub
    End Class
End Namespace
