using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record AuthenticatedUser(
    Guid Id,
    string DisplayName,
    string LoginName,
    string Email,
    UserRole Role,
    UserAccess Access);
