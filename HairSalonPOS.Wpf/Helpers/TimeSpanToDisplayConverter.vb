Imports System.Globalization
Imports System.Windows.Data

Namespace Helpers
    Public Class TimeSpanToDisplayConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse Not TypeOf value Is TimeSpan Then
                Return String.Empty
            End If

            Dim time = CType(value, TimeSpan)
            Dim dt = Date.Today.Add(time)
            Return dt.ToString("h:mm tt")
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
