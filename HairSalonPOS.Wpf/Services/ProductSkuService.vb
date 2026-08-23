Imports HairSalonPOS.Wpf.Models

Namespace Services
    ''' <summary>
    ''' CP### SKUs are legacy catalog entries from Master Files before unification.
    ''' All new products use P###.
    ''' </summary>
    Public NotInheritable Class ProductSkuService
        Private Sub New()
        End Sub

        Public Shared Function NextProductSku(products As IEnumerable(Of ProductItem)) As String
            Dim maxNum = 0
            For Each p In products
                If p.Sku Is Nothing OrElse p.Sku.Length < 2 Then Continue For
                If Not p.Sku.StartsWith("P", StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim n As Integer
                If Integer.TryParse(p.Sku.Substring(1), n) Then
                    maxNum = Math.Max(maxNum, n)
                End If
            Next
            Return $"P{(maxNum + 1):D3}"
        End Function
    End Class
End Namespace
