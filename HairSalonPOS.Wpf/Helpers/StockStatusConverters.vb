Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Media

Namespace Helpers
    Public Class StockStatusToBackgroundConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim status = TryCast(value, String)
            Select Case status
                Case "Out"
                    Return Application.Current.TryFindResource("ErrorBadgeBrush")
                Case "Low"
                    Return Application.Current.TryFindResource("WarningBadgeBrush")
                Case Else
                    Return Application.Current.TryFindResource("SuccessBadgeBrush")
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class

    Public Class StockStatusToForegroundConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim status = TryCast(value, String)
            Select Case status
                Case "Out"
                    Return Application.Current.TryFindResource("ErrorBrush")
                Case "Low"
                    Return New SolidColorBrush(Color.FromRgb(&H92, &H40, &H0))
                Case Else
                    Return New SolidColorBrush(Color.FromRgb(&H16, &H65, &H34))
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class

    Public Class StockLevelBarFillConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim status = TryCast(value, String)
            Select Case status
                Case "Out"
                    Return Application.Current.TryFindResource("ErrorBrush")
                Case "Low"
                    Return New SolidColorBrush(Color.FromRgb(&HD9, &H77, &H06))
                Case Else
                    Return Application.Current.TryFindResource("LinkStockInBrush")
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class

    Public Class RatioToWidthConverter
        Implements IMultiValueConverter

        Public Function Convert(values As Object(), targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert
            If values Is Nothing OrElse values.Length < 2 Then Return 0.0R
            Dim totalWidth = ToDouble(values(0))
            Dim ratio = ToDouble(values(1))
            If Not totalWidth.HasValue OrElse Not ratio.HasValue Then Return 0.0R
            Return Math.Max(0.0R, totalWidth.Value * ratio.Value)
        End Function

        Private Shared Function ToDouble(value As Object) As Double?
            If value Is Nothing Then Return Nothing
            If TypeOf value Is Double Then Return CDbl(value)
            Dim parsed As Double
            If Double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, parsed) Then
                Return parsed
            End If
            Return Nothing
        End Function

        Public Function ConvertBack(value As Object, targetTypes As Type(), parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
