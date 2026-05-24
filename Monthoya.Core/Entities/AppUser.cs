namespace Monthoya.Core.Entities;

public sealed class AppUser : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;

    public string LoginName { get; set; } = string.Empty;

    public string NormalizedLoginName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Usuario;

    public UserAccess Access { get; set; } = UserAccess.Dashboard
        | UserAccess.Properties
        | UserAccess.Contracts
        | UserAccess.Financial
        | UserAccess.Documents;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
