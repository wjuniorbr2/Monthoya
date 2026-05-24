using Monthoya.Core.Entities;

namespace Monthoya.Core.Security;

public static class RolePermissions
{
    public const UserAccess DefaultUserAccess = UserAccess.Dashboard
        | UserAccess.Properties
        | UserAccess.Contracts
        | UserAccess.Financial
        | UserAccess.Documents;

    public const UserAccess AdministratorAccess = DefaultUserAccess
        | UserAccess.UserManagement;

    public const UserAccess DeveloperAccess = AdministratorAccess
        | UserAccess.Diagnostics;

    public static bool CanManageUsers(UserRole role) =>
        GetEffectiveAccess(role, UserAccess.None).HasFlag(UserAccess.UserManagement);

    public static bool CanAccessDiagnostics(UserRole role) =>
        GetEffectiveAccess(role, UserAccess.None).HasFlag(UserAccess.Diagnostics);

    public static UserAccess GetEffectiveAccess(UserRole role, UserAccess assignedAccess) =>
        role switch
        {
            UserRole.Administrador => AdministratorAccess,
            UserRole.Desenvolvedor => DeveloperAccess,
            _ => assignedAccess & DefaultUserAccess
        };

    public static bool CanAccess(UserRole role, UserAccess assignedAccess, UserAccess requiredAccess) =>
        GetEffectiveAccess(role, assignedAccess).HasFlag(requiredAccess);
}
