namespace Monthoya.Core.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }

    public AppUser? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? Details { get; set; }
}
