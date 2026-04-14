using MongoDB.Bson.Serialization.Attributes;

namespace RagSystem.Infrastructure.Persistence.Documents;

public class DocumentDocument
{
    [BsonId] public string Id { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string Type { get; set; } = default!;
    public int Version { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public List<string> AllowedRoles { get; set; } = new();
    public string UploadedBy { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string ContentHash { get; set; } = default!;
}
