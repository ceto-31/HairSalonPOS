using HairSalonPOS.Models;

namespace HairSalonPOS.Helpers;

public static class SessionContext
{
    public static UserAccount? CurrentUser { get; private set; }

    public static void SetUser(UserAccount user) => CurrentUser = user;

    public static void Clear() => CurrentUser = null;

    public static bool IsLoggedIn => CurrentUser != null;

    public static bool HasRole(params string[] roles)
    {
        if (CurrentUser == null) return false;
        return roles.Contains(CurrentUser.RoleName, StringComparer.OrdinalIgnoreCase);
    }

    public static void RequireRole(params string[] roles)
    {
        if (!HasRole(roles))
            throw new UnauthorizedAccessException("You do not have permission to access this feature.");
    }
}
