Imports System.Globalization
Imports System.Windows.Data

Namespace Helpers
    Public Class HourToDisplayConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse Not TypeOf value Is Integer Then
                Return String.Empty
            End If

            Dim hour = CInt(value)
            If hour = 0 Then Return "12 AM"
            If hour < 12 Then Return $"{hour} AM"
            If hour = 12 Then Return "12 PM"
            Return $"{hour - 12} PM"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
