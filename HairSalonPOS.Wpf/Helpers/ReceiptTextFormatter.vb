Imports System.Text

Namespace Helpers
    Public Module ReceiptTextFormatter
        Public Function WrapText(text As String, maxWidth As Integer) As List(Of String)
            Dim result As New List(Of String)
            If String.IsNullOrEmpty(text) Then Return result
            If maxWidth <= 0 Then
                result.Add(text)
                Return result
            End If

            For Each paragraph In text.Replace(vbCrLf, vbLf).Split({vbLf}, StringSplitOptions.None)
                If paragraph.Length = 0 Then
                    result.Add(String.Empty)
                    Continue For
                End If

                Dim words = paragraph.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
                Dim currentLine As New StringBuilder()

                For Each word In words
                    If word.Length > maxWidth Then
                        If currentLine.Length > 0 Then
                            result.Add(currentLine.ToString())
                            currentLine.Clear()
                        End If

                        Dim remaining = word
                        While remaining.Length > maxWidth
                            result.Add(remaining.Substring(0, maxWidth))
                            remaining = remaining.Substring(maxWidth)
                        End While

                        If remaining.Length > 0 Then currentLine.Append(remaining)
                    ElseIf currentLine.Length = 0 Then
                        currentLine.Append(word)
                    ElseIf currentLine.Length + 1 + word.Length <= maxWidth Then
                        currentLine.Append(" "c).Append(word)
                    Else
                        result.Add(currentLine.ToString())
                        currentLine.Clear()
                        currentLine.Append(word)
                    End If
                Next

                If currentLine.Length > 0 Then result.Add(currentLine.ToString())
            Next

            Return result
        End Function

        Public Function FormatLeftRight(left As String, right As String, width As Integer) As String
            left = If(left, String.Empty)
            right = If(right, String.Empty)
            If width <= 0 Then Return left & right

            Dim maxLeft = Math.Max(0, width - right.Length)
            If left.Length > maxLeft Then left = left.Substring(0, maxLeft)

            Dim padding = width - left.Length - right.Length
            If padding < 1 Then padding = 1
            Return left & New String(" "c, padding) & right
        End Function

        Public Function FormatAmountLine(label As String, amount As Decimal, width As Integer) As String
            Return FormatLeftRight(label, amount.ToString("N2"), width)
        End Function

        Public Function FormatItemDetailLine(qty As Integer, unitPrice As Decimal, lineTotal As Decimal, width As Integer) As String
            Dim left = $"  {qty} x {unitPrice:N2} = "
            Return FormatLeftRight(left, lineTotal.ToString("N2"), width)
        End Function
    End Module
End Namespace
