Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class DiscountPersistenceService
        Private Shared ReadOnly _instance As New Lazy(Of DiscountPersistenceService)(Function() New DiscountPersistenceService())
        Private ReadOnly _discountsPath As String

        Public Shared ReadOnly Property Instance As DiscountPersistenceService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _discountsPath = Path.Combine(folder, "discounts.json")
        End Sub

        Public Function Load() As List(Of DiscountItem)
            If Not File.Exists(_discountsPath) Then Return Nothing
            Try
                Dim loaded = JsonSerializer.Deserialize(Of List(Of DiscountItem))(File.ReadAllText(_discountsPath))
                If loaded Is Nothing OrElse loaded.Count = 0 Then Return Nothing
                Return loaded
            Catch
                Return Nothing
            End Try
        End Function

        Public Sub Save(discounts As IEnumerable(Of DiscountItem))
            File.WriteAllText(_discountsPath, JsonSerializer.Serialize(discounts.ToList(), New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
