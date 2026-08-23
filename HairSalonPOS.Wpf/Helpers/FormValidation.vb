Imports System.Windows

Namespace Helpers
    Public Class FormValidation
        Public Shared ReadOnly HasErrorProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "HasError",
                GetType(Boolean),
                GetType(FormValidation),
                New FrameworkPropertyMetadata(False))

        Public Shared Sub SetHasError(element As DependencyObject, value As Boolean)
            element.SetValue(HasErrorProperty, value)
        End Sub

        Public Shared Function GetHasError(element As DependencyObject) As Boolean
            Return CBool(element.GetValue(HasErrorProperty))
        End Function
    End Class
End Namespace
