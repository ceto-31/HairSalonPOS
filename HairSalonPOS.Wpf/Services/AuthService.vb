Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class AuthService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance

        Public Function Authenticate(username As String, password As String) As UserAccount
            Return _store.Users.FirstOrDefault(
                Function(u) u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) AndAlso u.Password = password)
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
