using Monthoya.Core.Entities;

namespace Monthoya.Core.Security;

public static class RolePermissions
{
    public const UserAccess DefaultUserAccess = UserAccess.None;

    public const UserAccess AdministratorAccess = UserAccess.UserManagement;

    public const UserAccess DeveloperAccess = AdministratorAccess
        | UserAccess.Diagnostics;

    public static bool CanManageUsers(UserRole role, UserAccess assignedAccess = UserAccess.None) =>
        GetEffectiveAccess(role, assignedAccess).HasFlag(UserAccess.UserManagement);

    public static bool CanAccessDiagnostics(UserRole role) =>
        GetEffectiveAccess(role, UserAccess.None).HasFlag(UserAccess.Diagnostics);

    public static UserAccess GetEffectiveAccess(UserRole role, UserAccess assignedAccess) =>
        role switch
        {
            UserRole.Administrador => AdministratorAccess,
            UserRole.Desenvolvedor => DeveloperAccess,
            _ => assignedAccess & UserAccess.UserManagement
        };

    public static bool CanAccess(UserRole role, UserAccess assignedAccess, UserAccess requiredAccess) =>
        requiredAccess == UserAccess.Dashboard
            || GetEffectiveAccess(role, assignedAccess).HasFlag(requiredAccess);
}
