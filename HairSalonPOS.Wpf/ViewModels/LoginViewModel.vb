Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Services
Imports System.Linq

Namespace ViewModels
    Public Class LoginViewModel
        Inherits ViewModelBase

        Public Const StepLogin As String = "Login"
        Public Const StepUsername As String = "Username"
        Public Const StepSecurity As String = "Security"
        Public Const StepReset As String = "Reset"

        Private ReadOnly _auth As New AuthService()
        Private ReadOnly _onLoginSuccess As Action

        Private _username As String = String.Empty
        Private _password As String = String.Empty
        Private _errorMessage As String = String.Empty
        Private _recoveryStep As String = StepLogin
        Private _favNumber As String = String.Empty
        Private _favColor As String = String.Empty
        Private _favAnimal As String = String.Empty
        Private _favNumberError As String = String.Empty
        Private _favColorError As String = String.Empty
        Private _favAnimalError As String = String.Empty
        Private _newPassword As String = String.Empty
        Private _confirmPassword As String = String.Empty
        Private _infoMessage As String = String.Empty

        Public Sub New(onLoginSuccess As Action)
            _onLoginSuccess = onLoginSuccess
            LoginCommand = New RelayCommand(AddressOf ExecuteLogin, AddressOf CanLogin)
            ShowForgotPasswordCommand = New RelayCommand(AddressOf ShowForgotPassword)
            RecoveryBackCommand = New RelayCommand(AddressOf RecoveryBack)
            RecoveryNextCommand = New RelayCommand(AddressOf RecoveryNext, AddressOf CanRecoveryNext)
            ResetPasswordCommand = New RelayCommand(AddressOf ResetPassword, AddressOf CanResetPassword)
        End Sub

        Public ReadOnly Property PasswordRequirementsText As String
            Get
                Return PasswordValidator.RequirementsSummary
            End Get
        End Property

        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                SetProperty(_username, value)
                LoginCommand.NotifyCanExecuteChanged()
                RecoveryNextCommand.NotifyCanExecuteChanged()
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

        Public Property InfoMessage As String
            Get
                Return _infoMessage
            End Get
            Set(value As String)
                SetProperty(_infoMessage, value)
            End Set
        End Property

        Public Property RecoveryStep As String
            Get
                Return _recoveryStep
            End Get
            Set(value As String)
                If SetProperty(_recoveryStep, value) Then
                    OnPropertyChanged(NameOf(IsLoginStep))
                    OnPropertyChanged(NameOf(IsUsernameStep))
                    OnPropertyChanged(NameOf(IsSecurityStep))
                    OnPropertyChanged(NameOf(IsResetStep))
                    RecoveryNextCommand.NotifyCanExecuteChanged()
                    ResetPasswordCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public ReadOnly Property IsLoginStep As Boolean
            Get
                Return RecoveryStep = StepLogin
            End Get
        End Property

        Public ReadOnly Property IsUsernameStep As Boolean
            Get
                Return RecoveryStep = StepUsername
            End Get
        End Property

        Public ReadOnly Property IsSecurityStep As Boolean
            Get
                Return RecoveryStep = StepSecurity
            End Get
        End Property

        Public ReadOnly Property IsResetStep As Boolean
            Get
                Return RecoveryStep = StepReset
            End Get
        End Property

        Public Property FavNumber As String
            Get
                Return _favNumber
            End Get
            Set(value As String)
                If SetProperty(_favNumber, value) Then
                    FavNumberError = String.Empty
                    RecoveryNextCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property FavColor As String
            Get
                Return _favColor
            End Get
            Set(value As String)
                If SetProperty(_favColor, value) Then
                    FavColorError = String.Empty
                    RecoveryNextCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property FavAnimal As String
            Get
                Return _favAnimal
            End Get
            Set(value As String)
                If SetProperty(_favAnimal, value) Then
                    FavAnimalError = String.Empty
                    RecoveryNextCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property FavNumberError As String
            Get
                Return _favNumberError
            End Get
            Set(value As String)
                SetProperty(_favNumberError, value)
            End Set
        End Property

        Public Property FavColorError As String
            Get
                Return _favColorError
            End Get
            Set(value As String)
                SetProperty(_favColorError, value)
            End Set
        End Property

        Public Property FavAnimalError As String
            Get
                Return _favAnimalError
            End Get
            Set(value As String)
                SetProperty(_favAnimalError, value)
            End Set
        End Property

        Public Property NewPassword As String
            Get
                Return _newPassword
            End Get
            Set(value As String)
                SetProperty(_newPassword, value)
                ResetPasswordCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property ConfirmPassword As String
            Get
                Return _confirmPassword
            End Get
            Set(value As String)
                SetProperty(_confirmPassword, value)
                ResetPasswordCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property LoginCommand As RelayCommand
        Public Property ShowForgotPasswordCommand As RelayCommand
        Public Property RecoveryBackCommand As RelayCommand
        Public Property RecoveryNextCommand As RelayCommand
        Public Property ResetPasswordCommand As RelayCommand

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

        Private Sub ShowForgotPassword()
            ErrorMessage = String.Empty
            InfoMessage = String.Empty
            Username = String.Empty
            Password = String.Empty
            FavNumber = String.Empty
            FavColor = String.Empty
            FavAnimal = String.Empty
            ClearSecurityFieldErrors()
            NewPassword = String.Empty
            ConfirmPassword = String.Empty
            RecoveryStep = StepUsername
        End Sub

        Private Sub RecoveryBack()
            ErrorMessage = String.Empty
            InfoMessage = String.Empty
            ClearSecurityFieldErrors()
            Select Case RecoveryStep
                Case StepUsername
                    RecoveryStep = StepLogin
                Case StepSecurity
                    RecoveryStep = StepUsername
                Case StepReset
                    RecoveryStep = StepSecurity
                Case Else
                    RecoveryStep = StepLogin
            End Select
        End Sub

        Private Function CanRecoveryNext() As Boolean
            Select Case RecoveryStep
                Case StepUsername
                    Return Not String.IsNullOrWhiteSpace(Username)
                Case StepSecurity
                    Return Not String.IsNullOrWhiteSpace(FavNumber) AndAlso
                           Not String.IsNullOrWhiteSpace(FavColor) AndAlso
                           Not String.IsNullOrWhiteSpace(FavAnimal)
                Case Else
                    Return False
            End Select
        End Function

        Private Sub RecoveryNext()
            ErrorMessage = String.Empty
            InfoMessage = String.Empty
            ClearSecurityFieldErrors()

            Select Case RecoveryStep
                Case StepUsername
                    If _auth.FindUser(Username) Is Nothing Then
                        ErrorMessage = "Username not found."
                        Return
                    End If
                    InfoMessage = "Answer all three security questions to continue."
                    RecoveryStep = StepSecurity

                Case StepSecurity
                    Dim wrongAnswers = _auth.GetIncorrectSecurityAnswers(Username, FavNumber, FavColor, FavAnimal)
                    If wrongAnswers.Count > 0 Then
                        ErrorMessage = "One or more answers are incorrect."
                        Return
                    End If

                    NewPassword = String.Empty
                    ConfirmPassword = String.Empty
                    RecoveryStep = StepReset
            End Select
        End Sub

        Private Sub ClearSecurityFieldErrors()
            FavNumberError = String.Empty
            FavColorError = String.Empty
            FavAnimalError = String.Empty
        End Sub

        Private Function CanResetPassword() As Boolean
            Return RecoveryStep = StepReset AndAlso
                   Not String.IsNullOrWhiteSpace(NewPassword) AndAlso
                   Not String.IsNullOrWhiteSpace(ConfirmPassword)
        End Function

        Private Sub ResetPassword()
            ErrorMessage = String.Empty

            If NewPassword <> ConfirmPassword Then
                ErrorMessage = "Passwords do not match."
                Return
            End If

            Dim validation = PasswordValidator.Validate(NewPassword)
            If Not validation.IsValid Then
                ErrorMessage = "Password does not meet requirements:" & Environment.NewLine &
                               String.Join(Environment.NewLine, validation.Errors.Select(Function(e) "• " & e))
                Return
            End If

            If Not _auth.ResetPassword(Username, NewPassword) Then
                ErrorMessage = "Unable to reset password."
                Return
            End If

            Username = String.Empty
            Password = String.Empty
            InfoMessage = "Password updated. Sign in with your new password."
            RecoveryStep = StepLogin
        End Sub
    End Class
End Namespace
