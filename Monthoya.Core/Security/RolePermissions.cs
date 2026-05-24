using Monthoya.Core.Entities;

namespace Monthoya.Core.Security;

public static class RolePermissions
{
    public static bool CanManageUsers(UserRole role) =>
        role is UserRole.Administrador or UserRole.Desenvolvedor;

    public static bool CanAccessDiagnostics(UserRole role) =>
        role is UserRole.Desenvolvedor;
}
