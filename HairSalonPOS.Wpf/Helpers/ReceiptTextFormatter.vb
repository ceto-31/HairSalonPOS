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

        Public Function FormatWithAmountColumn(label As String, amountText As String, lineWidth As Integer, Optional amountColumnWidth As Integer = 10) As String
            label = If(label, String.Empty)
            amountText = If(amountText, String.Empty)
            If lineWidth <= 0 Then Return label & amountText

            Dim col = Math.Min(Math.Max(1, amountColumnWidth), lineWidth)
            If amountText.Length > col Then amountText = amountText.Substring(amountText.Length - col)
            amountText = amountText.PadLeft(col)

            Dim labelWidth = lineWidth - col
            If label.Length > labelWidth Then
                label = If(labelWidth <= 3, label.Substring(0, labelWidth), label.Substring(0, labelWidth - 3) & "...")
            End If

            Return label.PadRight(labelWidth) & amountText
        End Function

        Public Function FormatLeftRight(left As String, right As String, width As Integer, Optional amountColumnWidth As Integer = 0) As String
            If amountColumnWidth > 0 Then Return FormatWithAmountColumn(left, right, width, amountColumnWidth)

            left = If(left, String.Empty)
            right = If(right, String.Empty)
            If width <= 0 Then Return left & " " & right

            If right.Length >= width Then Return right.Substring(Math.Max(0, right.Length - width))

            Dim maxLeft = Math.Max(0, width - right.Length - 1)
            If left.Length > maxLeft Then
                left = If(maxLeft <= 3, left.Substring(0, maxLeft), left.Substring(0, maxLeft - 3) & "...")
            End If

            Dim padding = width - left.Length - right.Length
            If padding < 1 Then padding = 1
            Return left & New String(" "c, padding) & right
        End Function

        Public Function FormatAmountLine(label As String, amount As Decimal, width As Integer, Optional amountColumnWidth As Integer = 10) As String
            Return FormatWithAmountColumn(label, amount.ToString("N2"), width, amountColumnWidth)
        End Function

        Public Function FormatItemDetailLine(qty As Integer, unitPrice As Decimal, lineTotal As Decimal, width As Integer, Optional amountColumnWidth As Integer = 10) As String
            Dim left = $"  {qty} x {unitPrice:N2} = "
            Return FormatWithAmountColumn(left, lineTotal.ToString("N2"), width, amountColumnWidth)
        End Function
    End Module
End Namespace
