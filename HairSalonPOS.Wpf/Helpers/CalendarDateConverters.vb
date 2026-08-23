Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Helpers
    Public Class DateIsPastConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim day As Date? = Nothing
            If TypeOf value Is DateTime Then
                day = CType(value, DateTime).Date
            ElseIf TypeOf value Is Date Then
                day = CType(value, Date)
            End If
            If Not day.HasValue Then Return False
            Return day.Value.Date < Date.Today
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class

    Public Class DateHasAppointmentsConverter
        Implements IMultiValueConverter

        Public Function Convert(values As Object(), targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert
            If values Is Nothing OrElse values.Length < 2 Then Return Visibility.Collapsed

            Dim day As Date? = Nothing
            If TypeOf values(0) Is DateTime Then
                day = CType(values(0), DateTime).Date
            ElseIf TypeOf values(0) Is Date Then
                day = CType(values(0), Date)
            End If
            If Not day.HasValue Then Return Visibility.Collapsed

            Dim dates = TryCast(values(1), IEnumerable(Of Date))
            If dates Is Nothing Then Return Visibility.Collapsed

            Return If(dates.Any(Function(d) d.Date = day.Value), Visibility.Visible, Visibility.Collapsed)
        End Function

        Public Function ConvertBack(value As Object, targetTypes As Type(), parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
