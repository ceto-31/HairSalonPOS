Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class CatalogFileData
        Public Property Services As New List(Of ServiceItem)
        Public Property Products As New List(Of ProductItem)
    End Class

    Public Class CatalogPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of CatalogPersistenceService)(Function() New CatalogPersistenceService())
        Private ReadOnly _catalogPath As String

        Public Shared ReadOnly Property Instance As CatalogPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _catalogPath = Path.Combine(folder, "catalog.json")
        End Sub

        Public Function Load() As CatalogFileData
            If Not File.Exists(_catalogPath) Then Return Nothing
            Try
                Return JsonSerializer.Deserialize(Of CatalogFileData)(File.ReadAllText(_catalogPath))
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(services As IEnumerable(Of ServiceItem), products As IEnumerable(Of ProductItem))
            Dim data As New CatalogFileData With {
                .Services = services.ToList(),
                .Products = products.ToList()
            }
            File.WriteAllText(_catalogPath, JsonSerializer.Serialize(data, New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
