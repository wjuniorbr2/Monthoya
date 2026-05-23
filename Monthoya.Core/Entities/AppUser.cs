namespace Monthoya.Core.Entities;

public sealed class AppUser : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
