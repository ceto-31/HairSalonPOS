Imports System.Globalization
Imports System.Windows.Data

Namespace Helpers
    Public Class StringNotEmptyToBoolConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim text = TryCast(value, String)
            Return Not String.IsNullOrWhiteSpace(text)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
