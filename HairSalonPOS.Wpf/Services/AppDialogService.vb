Imports System.Windows

Namespace Services
    Public Class AppDialogOptions
        Public Property Title As String = String.Empty
        Public Property Message As String = String.Empty
        Public Property DialogType As AppDialogType = AppDialogType.Information
        Public Property Buttons As AppDialogButtons = AppDialogButtons.Ok
        Public Property PrimaryButtonText As String = String.Empty
        Public Property SecondaryButtonText As String = String.Empty
        Public Property Owner As Window
    End Class

    Public Module AppDialogService
        Public Function Show(message As String,
                             title As String,
                             buttons As AppDialogButtons,
                             dialogType As AppDialogType,
                             Optional owner As Window = Nothing) As AppDialogResult
            Return Show(New AppDialogOptions With {
                .Message = message,
                .Title = title,
                .Buttons = buttons,
                .DialogType = dialogType,
                .Owner = owner
            })
        End Function

        Public Function Show(options As AppDialogOptions) As AppDialogResult
            Dim dialog As New Views.AppDialogWindow(options)
            If options.Owner IsNot Nothing Then
                dialog.Owner = options.Owner
            ElseIf Application.Current?.MainWindow IsNot Nothing AndAlso Application.Current.MainWindow.IsLoaded Then
                dialog.Owner = Application.Current.MainWindow
            End If
            dialog.ShowDialog()
            Return dialog.Result
        End Function

        Public Sub ShowInfo(message As String, Optional title As String = "Information")
            Show(message, title, AppDialogButtons.Ok, AppDialogType.Information)
        End Sub

        Public Sub ShowSuccess(message As String, Optional title As String = "Success")
            Show(message, title, AppDialogButtons.Ok, AppDialogType.Success)
        End Sub

        Public Sub ShowWarning(message As String, Optional title As String = "Warning")
            Show(message, title, AppDialogButtons.Ok, AppDialogType.Warning)
        End Sub

        Public Sub ShowError(message As String, Optional title As String = "Error")
            Show(message, title, AppDialogButtons.Ok, AppDialogType.Error)
        End Sub

        Public Function Confirm(message As String,
                                title As String,
                                Optional primaryText As String = "Yes",
                                Optional secondaryText As String = "No",
                                Optional dialogType As AppDialogType = AppDialogType.Confirmation) As Boolean
            Return Show(New AppDialogOptions With {
                .Message = message,
                .Title = title,
                .Buttons = AppDialogButtons.YesNo,
                .DialogType = dialogType,
                .PrimaryButtonText = primaryText,
                .SecondaryButtonText = secondaryText
            }) = AppDialogResult.Yes
        End Function

        Public Function ConfirmDelete(itemName As String, Optional leadMessage As String = Nothing) As Boolean
            Dim body = $"Are you sure you want to delete ""{itemName}""? This action cannot be undone."
            If Not String.IsNullOrWhiteSpace(leadMessage) Then
                body = leadMessage.Trim() & Environment.NewLine & Environment.NewLine & body
            End If
            Return Show(New AppDialogOptions With {
                .Title = "Delete Item?",
                .Message = body,
                .Buttons = AppDialogButtons.YesNo,
                .DialogType = AppDialogType.Warning,
                .PrimaryButtonText = "Delete",
                .SecondaryButtonText = "Cancel"
            }) = AppDialogResult.Yes
        End Function
    End Module
End Namespace
