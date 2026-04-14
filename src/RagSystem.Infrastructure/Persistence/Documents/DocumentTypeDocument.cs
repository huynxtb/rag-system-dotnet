using MongoDB.Bson.Serialization.Attributes;

namespace RagSystem.Infrastructure.Persistence.Documents;

public class DocumentTypeDocument
{
    [BsonId] public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsBuiltIn { get; set; }
}
