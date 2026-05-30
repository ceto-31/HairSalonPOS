Namespace Helpers
    Public Class BooleanToVisibilityConverter
        Implements IValueConverter

        Public Property Invert As Boolean

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Dim flag = TypeOf value Is Boolean AndAlso CBool(value)
            If Invert Then flag = Not flag
            Return If(flag, Visibility.Visible, Visibility.Collapsed)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
