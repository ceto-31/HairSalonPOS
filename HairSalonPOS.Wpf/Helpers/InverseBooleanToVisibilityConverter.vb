Namespace Helpers
    Public Class InverseBooleanToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Dim flag = False
            If value IsNot Nothing AndAlso TypeOf value Is Boolean Then flag = CBool(value)
            Return If(flag, Visibility.Collapsed, Visibility.Visible)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return If(value IsNot Nothing AndAlso CType(value, Visibility) = Visibility.Visible, False, True)
        End Function
    End Class
End Namespace
