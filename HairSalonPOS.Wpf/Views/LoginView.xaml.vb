Namespace Views
    Partial Public Class LoginView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub PasswordBox_PasswordChanged(sender As Object, e As RoutedEventArgs)
            If DataContext IsNot Nothing Then
                CType(DataContext, ViewModels.LoginViewModel).Password = PasswordBox.Password
            End If
        End Sub
    End Class
End Namespace
