using Dapper;
using HairSalonPOS.Data;
using HairSalonPOS.Models;
using Microsoft.Data.SqlClient;

namespace HairSalonPOS.Services;

public class UserService
{
    public IEnumerable<UserAccount> GetAllUsers()
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.Query<UserAccount>(
            @"SELECT u.UserId, u.Username, u.FullName, u.RoleId, r.RoleName, u.IsActive, '' AS PasswordHash
              FROM Users u INNER JOIN Roles r ON u.RoleId = r.RoleId ORDER BY u.Username");
    }

    public IEnumerable<Role> GetRoles()
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        return conn.Query<Role>(
            "SELECT RoleId, RoleName FROM Roles WHERE RoleName IN ('Admin', 'Cashier') ORDER BY RoleId");
    }

    public void SaveUser(UserAccount user, string? newPassword)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();

        if (user.UserId == 0)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new InvalidOperationException("Password is required for new users.");

            var hash = Helpers.PasswordHasher.HashPassword(newPassword);
            conn.Execute(
                @"INSERT INTO Users (Username, PasswordHash, FullName, RoleId, IsActive)
                  VALUES (@Username, @PasswordHash, @FullName, @RoleId, @IsActive)",
                new { user.Username, PasswordHash = hash, user.FullName, user.RoleId, user.IsActive });
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var hash = Helpers.PasswordHasher.HashPassword(newPassword);
                conn.Execute(
                    @"UPDATE Users SET Username=@Username, FullName=@FullName, RoleId=@RoleId, IsActive=@IsActive, PasswordHash=@PasswordHash
                      WHERE UserId=@UserId",
                    new { user.Username, user.FullName, user.RoleId, user.IsActive, PasswordHash = hash, user.UserId });
            }
            else
            {
                conn.Execute(
                    @"UPDATE Users SET Username=@Username, FullName=@FullName, RoleId=@RoleId, IsActive=@IsActive
                      WHERE UserId=@UserId",
                    user);
            }
        }
    }

    public void DeactivateUser(int userId)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Execute("UPDATE Users SET IsActive = 0 WHERE UserId = @UserId", new { UserId = userId });
    }
}

public class BackupService
{
    public string GetDefaultBackupFolder()
    {
        var configured = System.Configuration.ConfigurationManager.AppSettings["BackupFolder"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        // SQL Server service account must have write access (avoid OneDrive Documents)
        var folder = @"C:\HairSalonBackups";
        Directory.CreateDirectory(folder);
        return folder;
    }

    public string CreateBackup(int userId, string? folder = null)
    {
        folder ??= GetDefaultBackupFolder();
        Directory.CreateDirectory(folder);
        var fileName = $"HairSalonDb_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(folder, fileName);

        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();

        using var cmd = new SqlCommand(
            "BACKUP DATABASE [HairSalon Db] TO DISK = @Path WITH FORMAT, INIT, NAME = @Name", conn);
        cmd.Parameters.AddWithValue("@Path", fullPath);
        cmd.Parameters.AddWithValue("@Name", $"HairSalon Backup {DateTime.Now}");
        cmd.ExecuteNonQuery();

        conn.Execute(
            "INSERT INTO BackupLog (BackupPath, UserId, Notes) VALUES (@BackupPath, @UserId, @Notes)",
            new { BackupPath = fullPath, UserId = userId, Notes = "Manual backup" });

        return fullPath;
    }

    public void RestoreBackup(string backupPath)
    {
        using var conn = SqlConnectionFactory.CreateConnection();
        conn.Open();

        using (var cmd = new SqlCommand(
            @"ALTER DATABASE [HairSalon Db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
              RESTORE DATABASE [HairSalon Db] FROM DISK = @Path WITH REPLACE;
              ALTER DATABASE [HairSalon Db] SET MULTI_USER;", conn))
        {
            cmd.CommandTimeout = 300;
            cmd.Parameters.AddWithValue("@Path", backupPath);
            cmd.ExecuteNonQuery();
        }
    }
}
