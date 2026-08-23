Imports System.Windows
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports HairSalonPOS.Wpf.Services

Namespace Views
    Partial Public Class AppDialogWindow
        Inherits Window

        Private ReadOnly _options As AppDialogOptions
        Private _isClosing As Boolean

        Public Property Result As AppDialogResult = AppDialogResult.None

        Public Sub New(options As AppDialogOptions)
            InitializeComponent()
            _options = options
            ApplyContent()
            ApplyButtons()
            ApplyIcon()
        End Sub

        Private Sub ApplyContent()
            TitleText.Text = If(String.IsNullOrWhiteSpace(_options.Title), "Notice", _options.Title)
            MessageText.Text = _options.Message
        End Sub

        Private Sub ApplyButtons()
            Select Case _options.Buttons
                Case AppDialogButtons.Ok
                    ButtonPanel.Visibility = Visibility.Collapsed
                    SingleButton.Visibility = Visibility.Visible
                    SingleButton.Content = If(String.IsNullOrWhiteSpace(_options.PrimaryButtonText), "OK", _options.PrimaryButtonText)
                Case AppDialogButtons.OkCancel
                    ButtonPanel.Visibility = Visibility.Visible
                    SingleButton.Visibility = Visibility.Collapsed
                    PrimaryButton.Content = If(String.IsNullOrWhiteSpace(_options.PrimaryButtonText), "OK", _options.PrimaryButtonText)
                    SecondaryButton.Content = If(String.IsNullOrWhiteSpace(_options.SecondaryButtonText), "Cancel", _options.SecondaryButtonText)
                Case AppDialogButtons.YesNo
                    ButtonPanel.Visibility = Visibility.Visible
                    SingleButton.Visibility = Visibility.Collapsed
                    PrimaryButton.Content = If(String.IsNullOrWhiteSpace(_options.PrimaryButtonText), "Yes", _options.PrimaryButtonText)
                    SecondaryButton.Content = If(String.IsNullOrWhiteSpace(_options.SecondaryButtonText), "No", _options.SecondaryButtonText)
            End Select
        End Sub

        Private Sub ApplyIcon()
            Select Case _options.DialogType
                Case AppDialogType.Success
                    IconCircle.Background = New SolidColorBrush(Color.FromRgb(&HE8, &HF0, &HE0))
                    IconText.Foreground = New SolidColorBrush(Color.FromRgb(&H22, &HA0, &H6B))
                    IconText.Text = "✓"
                Case AppDialogType.Error
                    IconCircle.Background = New SolidColorBrush(Color.FromRgb(&HF5, &HD5, &HD5))
                    IconText.Foreground = New SolidColorBrush(Color.FromRgb(&HA5, &H2A, &H2A))
                    IconText.Text = "✕"
                Case AppDialogType.Warning
                    IconCircle.Background = New SolidColorBrush(Color.FromRgb(&HF5, &HE6, &HC8))
                    IconText.Foreground = New SolidColorBrush(Color.FromRgb(&HC8, &HA9, &H7E))
                    IconText.Text = "!"
                Case AppDialogType.Information
                    IconCircle.Background = New SolidColorBrush(Color.FromRgb(&HE8, &HF0, &HFF))
                    IconText.Foreground = New SolidColorBrush(Color.FromRgb(&H25, &H63, &HEB))
                    IconText.Text = "i"
                Case Else
                    IconCircle.Background = New SolidColorBrush(Color.FromRgb(&HF5, &HF1, &HEB))
                    IconText.Foreground = New SolidColorBrush(Color.FromRgb(&H6B, &H44, &H23))
                    IconText.Text = "?"
            End Select
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            AppDialogService.ApplyOwnerOverlaySizing(Me)
            Dim storyboard = TryCast(TryFindResource("OpenStoryboard"), Storyboard)
            storyboard?.Begin(Me)
        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Escape Then
                e.Handled = True
                CloseWithResult(GetCancelResult())
            End If
        End Sub

        Private Sub PrimaryButton_Click(sender As Object, e As RoutedEventArgs)
            Select Case _options.Buttons
                Case AppDialogButtons.Ok, AppDialogButtons.OkCancel
                    CloseWithResult(AppDialogResult.Ok)
                Case AppDialogButtons.YesNo
                    CloseWithResult(AppDialogResult.Yes)
            End Select
        End Sub

        Private Sub SecondaryButton_Click(sender As Object, e As RoutedEventArgs)
            CloseWithResult(GetCancelResult())
        End Sub

        Private Function GetCancelResult() As AppDialogResult
            Select Case _options.Buttons
                Case AppDialogButtons.OkCancel
                    Return AppDialogResult.Cancel
                Case AppDialogButtons.YesNo
                    Return AppDialogResult.No
                Case Else
                    Return AppDialogResult.Ok
            End Select
        End Function

        Private Sub CloseWithResult(dialogResult As AppDialogResult)
            If _isClosing Then Return
            _isClosing = True
            Result = dialogResult

            Dim fadeOut As New DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
            Dim scaleOut As New DoubleAnimation(1, 0.96, TimeSpan.FromMilliseconds(120))

            AddHandler fadeOut.Completed, Sub()
                                              DialogResult = True
                                              Close()
                                          End Sub

            DialogCard.BeginAnimation(OpacityProperty, fadeOut)
            Overlay.BeginAnimation(OpacityProperty, New DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120)))
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut)
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut)
        End Sub
    End Class
End Namespace
