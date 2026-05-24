using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record UpdateUserRequest(
    Guid Id,
    string DisplayName,
    string Email,
    UserRole Role);
