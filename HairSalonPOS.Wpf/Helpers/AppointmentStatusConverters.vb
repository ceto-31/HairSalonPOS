Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Helpers
    Public Class AppointmentStatusToBadgeBrushConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim status = TryCast(value, String)
            Select Case status
                Case "Done"
                    Return Application.Current.TryFindResource("SuccessBadgeBrush")
                Case "No Show"
                    Return Application.Current.TryFindResource("ErrorBadgeBrush")
                Case Else
                    Return Application.Current.TryFindResource("WarningBadgeBrush")
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
