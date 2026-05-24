using Monthoya.Core.Entities;
using Monthoya.Core.Security;

namespace Monthoya.Core.Services;

public sealed record UpdateUserRequest(
    Guid Id,
    string DisplayName,
    string LoginName,
    string Email,
    UserRole Role,
    UserAccess Access = RolePermissions.DefaultUserAccess);
