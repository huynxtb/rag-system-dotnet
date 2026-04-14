using MongoDB.Bson.Serialization.Attributes;

namespace RagSystem.Infrastructure.Persistence.Documents;

public class ChatSessionDocument
{
    [BsonId] public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = "New chat";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDocument> Messages { get; set; } = new();
}

public class ChatMessageDocument
{
    public string Role { get; set; } = "User";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<ChatSourceDocument> Sources { get; set; } = new();
}

public class ChatSourceDocument
{
    public string DocumentId { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public int Version { get; set; }
    public int ChunkIndex { get; set; }
    public float Score { get; set; }
    public string Snippet { get; set; } = "";
}
