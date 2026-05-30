Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class LoginViewModel
        Inherits ViewModelBase

        Private ReadOnly _auth As New AuthService()
        Private ReadOnly _onLoginSuccess As Action

        Private _username As String = String.Empty
        Private _password As String = String.Empty
        Private _errorMessage As String = String.Empty

        Public Sub New(onLoginSuccess As Action)
            _onLoginSuccess = onLoginSuccess
            LoginCommand = New RelayCommand(AddressOf ExecuteLogin, AddressOf CanLogin)
        End Sub

        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                SetProperty(_username, value)
                LoginCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property Password As String
            Get
                Return _password
            End Get
            Set(value As String)
                SetProperty(_password, value)
                LoginCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Public Property LoginCommand As RelayCommand

        Private Function CanLogin() As Boolean
            Return Not String.IsNullOrWhiteSpace(Username) AndAlso Not String.IsNullOrWhiteSpace(Password)
        End Function

        Private Sub ExecuteLogin()
            ErrorMessage = String.Empty
            Dim user = _auth.Authenticate(Username.Trim(), Password)
            If user Is Nothing Then
                ErrorMessage = "Invalid username or password."
                Return
            End If

            SessionContext.CurrentUser = user
            _onLoginSuccess.Invoke()
        End Sub
    End Class
End Namespace
