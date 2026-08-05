Namespace Views
    Partial Public Class LoginView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler DataContextChanged, AddressOf LoginView_DataContextChanged
        End Sub

        Private Sub LoginView_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            Dim oldVm = TryCast(e.OldValue, ViewModels.LoginViewModel)
            If oldVm IsNot Nothing Then
                RemoveHandler oldVm.PropertyChanged, AddressOf ViewModel_PropertyChanged
            End If

            Dim newVm = TryCast(e.NewValue, ViewModels.LoginViewModel)
            If newVm IsNot Nothing Then
                AddHandler newVm.PropertyChanged, AddressOf ViewModel_PropertyChanged
            End If
        End Sub

        Private Sub ViewModel_PropertyChanged(sender As Object, e As ComponentModel.PropertyChangedEventArgs)
            Dim vm = TryCast(sender, ViewModels.LoginViewModel)
            If vm Is Nothing Then Return

            Select Case e.PropertyName
                Case NameOf(ViewModels.LoginViewModel.RecoveryStep)
                    If vm.RecoveryStep = ViewModels.LoginViewModel.StepUsername OrElse
                       vm.RecoveryStep = ViewModels.LoginViewModel.StepLogin Then
                        ClearPasswordFields()
                    End If
                    If vm.RecoveryStep = ViewModels.LoginViewModel.StepReset Then
                        ClearResetPasswordFields()
                    End If
                    Dispatcher.BeginInvoke(Sub() FocusStepInput(vm), System.Windows.Threading.DispatcherPriority.Input)

                Case NameOf(ViewModels.LoginViewModel.Password)
                    If String.IsNullOrEmpty(vm.Password) AndAlso PasswordBox.Password <> String.Empty Then
                        PasswordBox.Password = String.Empty
                        PasswordTextBox.Text = String.Empty
                    End If
            End Select
        End Sub

        Private Sub FocusStepInput(vm As ViewModels.LoginViewModel)
            Select Case vm.RecoveryStep
                Case ViewModels.LoginViewModel.StepLogin
                    LoginUsernameBox?.Focus()
                    Keyboard.Focus(LoginUsernameBox)
                Case ViewModels.LoginViewModel.StepUsername
                    RecoveryUsernameBox?.Focus()
                    Keyboard.Focus(RecoveryUsernameBox)
            End Select
        End Sub

        Private Sub ClearPasswordFields()
            PasswordBox.Password = String.Empty
            PasswordTextBox.Text = String.Empty
        End Sub

        Private Sub ClearResetPasswordFields()
            NewPasswordBox.Password = String.Empty
            NewPasswordTextBox.Text = String.Empty
            ConfirmPasswordBox.Password = String.Empty
            ConfirmPasswordTextBox.Text = String.Empty
        End Sub

        Private Sub PasswordBox_PasswordChanged(sender As Object, e As RoutedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If PasswordBox.Visibility = Visibility.Visible Then
                vm.Password = PasswordBox.Password
            End If
        End Sub

        Private Sub PasswordTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If PasswordTextBox.Visibility = Visibility.Visible Then
                vm.Password = PasswordTextBox.Text
            End If
        End Sub

        Private Sub NewPasswordBox_PasswordChanged(sender As Object, e As RoutedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If NewPasswordBox.Visibility = Visibility.Visible Then
                vm.NewPassword = NewPasswordBox.Password
            End If
        End Sub

        Private Sub NewPasswordTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If NewPasswordTextBox.Visibility = Visibility.Visible Then
                vm.NewPassword = NewPasswordTextBox.Text
            End If
        End Sub

        Private Sub ConfirmPasswordBox_PasswordChanged(sender As Object, e As RoutedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If ConfirmPasswordBox.Visibility = Visibility.Visible Then
                vm.ConfirmPassword = ConfirmPasswordBox.Password
            End If
        End Sub

        Private Sub ConfirmPasswordTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            If DataContext Is Nothing Then Return
            Dim vm = CType(DataContext, ViewModels.LoginViewModel)
            If ConfirmPasswordTextBox.Visibility = Visibility.Visible Then
                vm.ConfirmPassword = ConfirmPasswordTextBox.Text
            End If
        End Sub

        Private Sub PasswordReveal_Checked(sender As Object, e As RoutedEventArgs)
            PasswordTextBox.Text = PasswordBox.Password
            PasswordBox.Visibility = Visibility.Collapsed
            PasswordTextBox.Visibility = Visibility.Visible
        End Sub

        Private Sub PasswordReveal_Unchecked(sender As Object, e As RoutedEventArgs)
            PasswordBox.Password = PasswordTextBox.Text
            PasswordTextBox.Visibility = Visibility.Collapsed
            PasswordBox.Visibility = Visibility.Visible
        End Sub

        Private Sub NewPasswordReveal_Checked(sender As Object, e As RoutedEventArgs)
            NewPasswordTextBox.Text = NewPasswordBox.Password
            NewPasswordBox.Visibility = Visibility.Collapsed
            NewPasswordTextBox.Visibility = Visibility.Visible
        End Sub

        Private Sub NewPasswordReveal_Unchecked(sender As Object, e As RoutedEventArgs)
            NewPasswordBox.Password = NewPasswordTextBox.Text
            NewPasswordTextBox.Visibility = Visibility.Collapsed
            NewPasswordBox.Visibility = Visibility.Visible
        End Sub

        Private Sub ConfirmPasswordReveal_Checked(sender As Object, e As RoutedEventArgs)
            ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password
            ConfirmPasswordBox.Visibility = Visibility.Collapsed
            ConfirmPasswordTextBox.Visibility = Visibility.Visible
        End Sub

        Private Sub ConfirmPasswordReveal_Unchecked(sender As Object, e As RoutedEventArgs)
            ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text
            ConfirmPasswordTextBox.Visibility = Visibility.Collapsed
            ConfirmPasswordBox.Visibility = Visibility.Visible
        End Sub
    End Class
End Namespace
