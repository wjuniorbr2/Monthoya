namespace Monthoya.Core.Entities;

public sealed class Person : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public PersonRole Role { get; set; } = PersonRole.Client;

    public string? Notes { get; set; }
}
