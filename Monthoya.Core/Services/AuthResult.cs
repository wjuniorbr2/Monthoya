namespace Monthoya.Core.Services;

public sealed record AuthResult(
    bool Succeeded,
    AuthenticatedUser? User,
    string? ErrorMessage);
