Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Helpers
    Public Class StringEmptyToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim text = TryCast(value, String)
            Return If(String.IsNullOrWhiteSpace(text), Visibility.Visible, Visibility.Collapsed)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
