Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class BirthdatePromptWindow
        Inherits Window

        Public Property SelectedBirthdate As Date?
        Public Property Confirmed As Boolean

        Public Sub New(Optional initialDate As Date? = Nothing)
            InitializeComponent()
            If initialDate.HasValue Then
                BirthdatePicker.SelectedDate = initialDate.Value
            End If
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
            Dispatcher.BeginInvoke(Sub()
                                       BirthdatePicker.IsDropDownOpen = True
                                       BirthdatePicker.Focus()
                                   End Sub)
        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Escape Then
                CancelSelection()
                e.Handled = True
            ElseIf e.Key = Key.Enter Then
                ConfirmSelection()
                e.Handled = True
            End If
        End Sub

        Private Sub Confirm_Click(sender As Object, e As RoutedEventArgs)
            ConfirmSelection()
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            CancelSelection()
        End Sub

        Private Sub ConfirmSelection()
            If Not BirthdatePicker.SelectedDate.HasValue Then
                BirthdatePicker.IsDropDownOpen = True
                BirthdatePicker.Focus()
                Return
            End If
            SelectedBirthdate = BirthdatePicker.SelectedDate.Value.Date
            Confirmed = True
            DialogResult = True
            Close()
        End Sub

        Private Sub CancelSelection()
            SelectedBirthdate = Nothing
            Confirmed = False
            DialogResult = False
            Close()
        End Sub
    End Class
End Namespace
