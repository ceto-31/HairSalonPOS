Imports System.Linq

Namespace Services
    Public Class PasswordValidationResult
        Public Property IsValid As Boolean
        Public Property Errors As New List(Of String)
    End Class

    Public Class PasswordValidator
        Public Shared ReadOnly Property RequirementsSummary As String
            Get
                Return "Use at least 8 characters with uppercase, lowercase, a number, and a special character."
            End Get
        End Property

        Public Shared Function Validate(password As String) As PasswordValidationResult
            Dim result As New PasswordValidationResult With {.IsValid = True}

            If String.IsNullOrEmpty(password) Then
                result.IsValid = False
                result.Errors.Add("Password is required.")
                Return result
            End If

            If password.Length < 8 Then
                result.IsValid = False
                result.Errors.Add("At least 8 characters")
            End If

            If Not password.Any(Function(c) Char.IsUpper(c)) Then
                result.IsValid = False
                result.Errors.Add("At least one uppercase letter")
            End If

            If Not password.Any(Function(c) Char.IsLower(c)) Then
                result.IsValid = False
                result.Errors.Add("At least one lowercase letter")
            End If

            If Not password.Any(Function(c) Char.IsDigit(c)) Then
                result.IsValid = False
                result.Errors.Add("At least one number")
            End If

            If Not password.Any(Function(c) Not Char.IsLetterOrDigit(c)) Then
                result.IsValid = False
                result.Errors.Add("At least one special character")
            End If

            Return result
        End Function
    End Class
End Namespace
