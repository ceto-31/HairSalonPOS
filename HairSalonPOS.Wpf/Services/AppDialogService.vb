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

        Public Function ConfirmUseReserveStock(shortfalls As IList(Of ConsumableStockShortfall),
                                               Optional owner As Window = Nothing) As Boolean
            If shortfalls Is Nothing OrElse shortfalls.Count = 0 Then Return True

            Dim body As New System.Text.StringBuilder()
            If shortfalls.Count = 1 Then
                Dim item = shortfalls(0)
                Dim product = item.Product
                If item.FromOnHand <= 0 Then
                    body.AppendLine($"{product.Name} — stock on hand is 0. This sale needs {item.UnitsNeeded} units.")
                    body.AppendLine($"Use {item.FromReserve} from reserve stock? ({product.ReservedQty} in reserve)")
                Else
                    body.AppendLine($"{product.Name} — only {item.FromOnHand} on hand, need {item.FromReserve} more.")
                    body.AppendLine($"Use {item.FromReserve} from reserve stock? ({product.ReservedQty} in reserve)")
                End If
            Else
                body.AppendLine("Some products need reserve stock:")
                For Each item In shortfalls
                    Dim onHandLabel = If(item.FromOnHand <= 0, "0 on hand", $"{item.FromOnHand} on hand")
                    body.AppendLine($"• {item.Product.Name}: {onHandLabel}, use {item.FromReserve} from reserve")
                Next
                body.AppendLine()
                body.AppendLine("Use reserve stock for this sale?")
            End If

            Return Show(New AppDialogOptions With {
                .Title = "Use reserve stock?",
                .Message = body.ToString().Trim(),
                .Buttons = AppDialogButtons.YesNo,
                .DialogType = AppDialogType.Warning,
                .PrimaryButtonText = "Use reserve stock",
                .SecondaryButtonText = "Cancel",
                .Owner = owner
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

        Public Function PromptBirthdate(Optional initialDate As Date? = Nothing,
                                        Optional owner As Window = Nothing) As Date?
            Dim dialog As New Views.BirthdatePromptWindow(initialDate)
            If owner IsNot Nothing Then
                dialog.Owner = owner
            ElseIf Application.Current?.MainWindow IsNot Nothing AndAlso Application.Current.MainWindow.IsLoaded Then
                dialog.Owner = Application.Current.MainWindow
            End If
            Dim result = dialog.ShowDialog()
            If result = True AndAlso dialog.Confirmed AndAlso dialog.SelectedBirthdate.HasValue Then
                Return dialog.SelectedBirthdate
            End If
            Return Nothing
        End Function

        Public Function PromptStockMovement(product As Models.ProductItem,
                                            isStockIn As Boolean,
                                            Optional owner As Window = Nothing,
                                            Optional initialQty As Integer = 1) As StockMovementPromptResult
            If product Is Nothing Then Return Nothing

            Try
                product.EnsureDefaults()
            Catch ex As Exception
                ErrorLogService.LogException("PromptStockMovement/EnsureDefaults", ex)
                Throw
            End Try

            If Not isStockIn AndAlso product.StockOnHand <= 0 Then
                ShowWarning($"{If(product.Name, "This product")} has no stock to issue.", "Stock out")
                Return Nothing
            End If

            Dim dialog As Views.StockMovementWindow
            Try
                dialog = New Views.StockMovementWindow(product, isStockIn, initialQty)
            Catch ex As Exception
                ErrorLogService.LogException("PromptStockMovement/ConstructWindow", ex)
                Throw
            End Try

            Try
                Dim ownerWin = owner
                If ownerWin Is Nothing AndAlso Application.Current?.MainWindow IsNot Nothing AndAlso Application.Current.MainWindow.IsLoaded Then
                    ownerWin = Application.Current.MainWindow
                End If
                If ownerWin IsNot Nothing Then
                    dialog.Owner = ownerWin
                    SizeDialogToOwner(dialog, ownerWin)
                End If
            Catch ex As Exception
                ErrorLogService.LogException("PromptStockMovement/OwnerSizing", ex)
                Throw
            End Try

            Dim result As Boolean?
            Try
                result = dialog.ShowDialog()
            Catch ex As Exception
                ErrorLogService.LogException("PromptStockMovement/ShowDialog", ex)
                Throw
            End Try

            If result = True AndAlso dialog.Confirmed AndAlso dialog.LoadSucceeded Then
                Return New StockMovementPromptResult With {
                    .Quantity = dialog.ResultQuantity,
                    .Reason = dialog.ResultReason,
                    .Notes = dialog.ResultNotes
                }
            End If
            Return Nothing
        End Function

        Public Function PromptReserveStock(product As Models.ProductItem,
                                           Optional owner As Window = Nothing,
                                           Optional initialQty As Integer = 1) As StockMovementPromptResult
            If product Is Nothing Then Return Nothing

            Try
                product.EnsureDefaults()
            Catch ex As Exception
                ErrorLogService.LogException("PromptReserveStock/EnsureDefaults", ex)
                Throw
            End Try

            Dim dialog As Views.StockMovementWindow
            Try
                dialog = New Views.StockMovementWindow(product, StockMovementKind.Reserve, initialQty)
            Catch ex As Exception
                ErrorLogService.LogException("PromptReserveStock/ConstructWindow", ex)
                Throw
            End Try

            Try
                Dim ownerWin = owner
                If ownerWin Is Nothing AndAlso Application.Current?.MainWindow IsNot Nothing AndAlso Application.Current.MainWindow.IsLoaded Then
                    ownerWin = Application.Current.MainWindow
                End If
                If ownerWin IsNot Nothing Then
                    dialog.Owner = ownerWin
                    SizeDialogToOwner(dialog, ownerWin)
                End If
            Catch ex As Exception
                ErrorLogService.LogException("PromptReserveStock/OwnerSizing", ex)
                Throw
            End Try

            Dim result As Boolean?
            Try
                result = dialog.ShowDialog()
            Catch ex As Exception
                ErrorLogService.LogException("PromptReserveStock/ShowDialog", ex)
                Throw
            End Try

            If result = True AndAlso dialog.Confirmed AndAlso dialog.LoadSucceeded Then
                Return New StockMovementPromptResult With {
                    .Quantity = dialog.ResultQuantity,
                    .Reason = dialog.ResultReason,
                    .Notes = dialog.ResultNotes,
                    .IsReleaseReserve = dialog.ResultIsReleaseReserve
                }
            End If
            Return Nothing
        End Function

        Public Function PromptServiceConsumables(service As Models.ServiceItem,
                                                 Optional owner As Window = Nothing) As List(Of Models.ServiceConsumableLine)
            If service Is Nothing OrElse Not service.HasPickOneConsumables Then Return Nothing

            Dim dialog As Views.ServiceConsumablePickerWindow
            Try
                dialog = New Views.ServiceConsumablePickerWindow(service)
            Catch ex As Exception
                ErrorLogService.LogException("PromptServiceConsumables/ConstructWindow", ex)
                Throw
            End Try

            Try
                Dim ownerWin = owner
                If ownerWin Is Nothing AndAlso Application.Current?.MainWindow IsNot Nothing AndAlso Application.Current.MainWindow.IsLoaded Then
                    ownerWin = Application.Current.MainWindow
                End If
                If ownerWin IsNot Nothing Then
                    dialog.Owner = ownerWin
                    SizeDialogToOwner(dialog, ownerWin)
                End If
            Catch ex As Exception
                ErrorLogService.LogException("PromptServiceConsumables/OwnerSizing", ex)
                Throw
            End Try

            Dim result As Boolean?
            Try
                result = dialog.ShowDialog()
            Catch ex As Exception
                ErrorLogService.LogException("PromptServiceConsumables/ShowDialog", ex)
                Throw
            End Try

            If result = True AndAlso dialog.Confirmed Then
                Return dialog.Selections
            End If
            Return Nothing
        End Function

        Private Function TrySizeDialogToOwner(dialog As Window, ownerWin As Window) As Boolean
            Dim ownerWidth = If(ownerWin.ActualWidth > 0, ownerWin.ActualWidth, ownerWin.Width)
            Dim ownerHeight = If(ownerWin.ActualHeight > 0, ownerWin.ActualHeight, ownerWin.Height)
            If Double.IsNaN(ownerWidth) OrElse Double.IsNaN(ownerHeight) OrElse
               Double.IsNaN(ownerWin.Left) OrElse Double.IsNaN(ownerWin.Top) Then
                Return False
            End If

            dialog.WindowStartupLocation = WindowStartupLocation.Manual
            dialog.Width = Math.Max(ownerWidth, 400)
            dialog.Height = Math.Max(ownerHeight, 300)
            dialog.Left = ownerWin.Left
            dialog.Top = ownerWin.Top
            Return True
        End Function

        Private Sub SizeDialogToWorkArea(dialog As Window)
            Dim area = SystemParameters.WorkArea
            dialog.WindowStartupLocation = WindowStartupLocation.Manual
            dialog.Width = area.Width
            dialog.Height = area.Height
            dialog.Left = area.Left
            dialog.Top = area.Top
        End Sub

        Private Sub SizeDialogToOwner(dialog As Window, ownerWin As Window)
            If Not TrySizeDialogToOwner(dialog, ownerWin) Then
                SizeDialogToWorkArea(dialog)
            End If
        End Sub

        ''' <summary>
        ''' Stretches a dialog over its owner so its scrim reads as a full-window dim rather than a
        ''' rectangle hugging the rounded card.
        ''' </summary>
        Public Sub ApplyOwnerOverlaySizing(dialog As Window)
            If dialog Is Nothing Then Return

            Dim ownerWin = dialog.Owner
            If ownerWin Is Nothing AndAlso
               Application.Current?.MainWindow IsNot Nothing AndAlso
               Application.Current.MainWindow.IsLoaded AndAlso
               Not ReferenceEquals(Application.Current.MainWindow, dialog) Then
                ownerWin = Application.Current.MainWindow
            End If

            ' Without explicit bounds the window keeps its NaN size and collapses, so an
            ' unmeasurable owner (or none at all, as on the login screen) falls back to the
            ' work area. Either way the scrim covers a full window rather than hugging the card.
            If ownerWin Is Nothing OrElse Not TrySizeDialogToOwner(dialog, ownerWin) Then
                SizeDialogToWorkArea(dialog)
            End If
        End Sub
    End Module

    Public Class StockMovementPromptResult
        Public Property Quantity As Integer
        Public Property Reason As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property IsReleaseReserve As Boolean

        Public ReadOnly Property CombinedNotes As String
            Get
                If String.IsNullOrWhiteSpace(Notes) Then Return Reason
                If String.IsNullOrWhiteSpace(Reason) Then Return Notes
                Return $"{Reason}. {Notes.Trim()}"
            End Get
        End Property
    End Class
End Namespace
