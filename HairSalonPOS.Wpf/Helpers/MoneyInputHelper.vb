Imports System.Globalization

Namespace Helpers
    Public Module MoneyInputHelper
        Public Function FormatAmount(amount As Decimal) As String
            Return amount.ToString("N2", CultureInfo.CurrentCulture)
        End Function

        Public Function TryParseAmount(text As String, ByRef amount As Decimal) As Boolean
            If String.IsNullOrWhiteSpace(text) Then
                amount = 0D
                Return True
            End If

            Dim cleaned = text.Trim().
                Replace("₱", String.Empty).
                Replace(",", String.Empty).
                Trim()

            Return Decimal.TryParse(cleaned, NumberStyles.Number Or NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, amount)
        End Function
    End Module
End Namespace
