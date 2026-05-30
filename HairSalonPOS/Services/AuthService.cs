using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Helpers;
using HairSalonPOS.Models;

namespace HairSalonPOS.Services;

public class AuthService
{
    public UserAccount? Authenticate(string username, string password)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();

        var user = conn.QuerySingleOrDefault<UserAccount>(
            @"SELECT u.UserId, u.Username, u.PasswordHash, u.FullName, u.RoleId, r.RoleName, u.IsActive
              FROM Users u INNER JOIN Roles r ON u.RoleId = r.RoleId
              WHERE u.Username = @Username AND u.IsActive = 1",
            new { Username = username });

        if (user == null) return null;

        if (user.PasswordHash.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "Admin@123")
            {
                var hash = PasswordHasher.HashPassword(password);
                conn.Execute("UPDATE Users SET PasswordHash = @Hash WHERE UserId = @UserId",
                    new { Hash = hash, user.UserId });
                user.PasswordHash = hash;
                return user;
            }
            return null;
        }

        return PasswordHasher.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public static void EnsureDefaultPasswords()
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();

        var users = conn.Query<UserAccount>(
            "SELECT UserId, Username, PasswordHash FROM Users WHERE PasswordHash LIKE '%placeholder%'").ToList();

        foreach (var user in users)
        {
            var defaultPassword = user.Username.ToLowerInvariant() switch
            {
                "admin" => "Admin@123",
                "manager" => "Manager@123",
                "cashier" => "Cashier@123",
                _ => "Admin@123"
            };
            var hash = PasswordHasher.HashPassword(defaultPassword);
            conn.Execute("UPDATE Users SET PasswordHash = @Hash WHERE UserId = @UserId",
                new { Hash = hash, user.UserId });
        }
    }
}
