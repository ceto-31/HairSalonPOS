Imports System.Globalization
Imports System.Windows.Data

Namespace Helpers
    Public Class IntegerAtMostConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim threshold As Integer = 3
            If parameter IsNot Nothing Then Integer.TryParse(parameter.ToString(), threshold)

            Dim number As Integer
            If value Is Nothing OrElse Not Integer.TryParse(value.ToString(), number) Then Return False

            Return number <= threshold
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
