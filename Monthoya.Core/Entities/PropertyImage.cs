namespace Monthoya.Core.Entities;

public sealed class PropertyImage : BaseEntity
{
    public Guid PropertyId { get; set; }

    public Property? Property { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public int DisplayOrder { get; set; }
}
