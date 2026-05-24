using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

internal static class UserMapper
{
    public static UserSummary ToSummary(AppUser user) =>
        new(user.Id, user.DisplayName, user.LoginName, user.Email, user.Role, user.IsActive, user.LastLoginAtUtc);

    public static AuthenticatedUser ToAuthenticatedUser(AppUser user) =>
        new(user.Id, user.DisplayName, user.LoginName, user.Email, user.Role);
}
