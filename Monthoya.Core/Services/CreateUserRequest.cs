using Monthoya.Core.Entities;
using Monthoya.Core.Security;

namespace Monthoya.Core.Services;

public sealed record CreateUserRequest(
    string DisplayName,
    string LoginName,
    string Email,
    string Password,
    UserRole Role,
    UserAccess Access = RolePermissions.DefaultUserAccess);
