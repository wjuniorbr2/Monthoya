using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record UserSummary(
    Guid Id,
    string DisplayName,
    string LoginName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);
