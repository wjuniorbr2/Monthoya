using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record CreateUserRequest(
    string DisplayName,
    string LoginName,
    string Email,
    string Password,
    UserRole Role);
