namespace Monthoya.Core.Entities;

public sealed class Document : BaseEntity
{
    public string RelatedEntityType { get; set; } = string.Empty;

    public Guid? RelatedEntityId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }
}
