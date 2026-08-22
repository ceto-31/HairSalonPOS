Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module ProductPlaceholderIcons
        Public Function Resolve(product As ProductItem) As String
            If product Is Nothing Then Return "📦"
            Return ResolveFromText($"{product.Name} {product.Brand} {product.Category} {product.SubCategory}")
        End Function

        Public Function ResolveFromText(text As String) As String
            If String.IsNullOrWhiteSpace(text) Then Return "📦"
            Dim normalized = text.ToLowerInvariant()

            If ContainsAny(normalized, "spray", "mist", "hairspray") Then Return "💨"
            If ContainsAny(normalized, "tube", "color", "colour", "dye", "tint", "bleach") Then Return "🧴"
            If ContainsAny(normalized, "shampoo", "conditioner", "bottle", "wash") Then Return "🧴"
            If ContainsAny(normalized, "serum", "oil", "drop", "essence") Then Return "💧"
            If ContainsAny(normalized, "wax", "cream", "lotion", "mask", "treatment") Then Return "🫙"
            If ContainsAny(normalized, "comb", "brush", "tool") Then Return "🪮"
            If ContainsAny(normalized, "nail", "polish") Then Return "💅"
            Return "📦"
        End Function

        Private Function ContainsAny(normalized As String, ParamArray terms As String()) As Boolean
            For Each term In terms
                If normalized.Contains(term) Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace
