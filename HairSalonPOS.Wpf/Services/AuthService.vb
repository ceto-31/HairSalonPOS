Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class AuthService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Public Function Authenticate(username As String, password As String) As UserAccount
            Return _store.Users.FirstOrDefault(
                Function(u) u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) AndAlso u.Password = password)
        End Function

        Public Function FindUser(username As String) As UserAccount
            If String.IsNullOrWhiteSpace(username) Then Return Nothing
            Return _store.Users.FirstOrDefault(
                Function(u) u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
        End Function

        Public Function VerifySecurityAnswers(username As String, favNumber As String, favColor As String, favAnimal As String) As Boolean
            Return GetIncorrectSecurityAnswers(username, favNumber, favColor, favAnimal).Count = 0
        End Function

        Public Function GetIncorrectSecurityAnswers(username As String, favNumber As String, favColor As String, favAnimal As String) As List(Of String)
            Dim wrong As New List(Of String)
            Dim user = FindUser(username)
            If user Is Nothing Then Return wrong

            If Not String.Equals(user.FavNumber?.Trim(), favNumber?.Trim(), StringComparison.OrdinalIgnoreCase) Then
                wrong.Add("Favorite number")
            End If

            If Not String.Equals(user.FavColor?.Trim(), favColor?.Trim(), StringComparison.OrdinalIgnoreCase) Then
                wrong.Add("Favorite color")
            End If

            If Not String.Equals(user.FavAnimal?.Trim(), favAnimal?.Trim(), StringComparison.OrdinalIgnoreCase) Then
                wrong.Add("Favorite animal")
            End If

            Return wrong
        End Function

        Public Function ResetPassword(username As String, newPassword As String) As Boolean
            Dim user = FindUser(username)
            If user Is Nothing OrElse String.IsNullOrWhiteSpace(newPassword) Then Return False
            user.Password = newPassword
            _store.PersistUsers()
            Return True
        End Function
    End Class

    Public Class SessionContext
        Public Shared Property CurrentUser As UserAccount

        Public Shared ReadOnly Property IsAdmin As Boolean
            Get
                Return CurrentUser IsNot Nothing AndAlso CurrentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            End Get
        End Property
    End Class
End Namespace
