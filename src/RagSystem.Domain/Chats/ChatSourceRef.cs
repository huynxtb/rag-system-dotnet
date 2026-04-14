namespace RagSystem.Domain.Chats;

/// <summary>Citation metadata attached to an assistant message.</summary>
public record ChatSourceRef(
    string DocumentId,
    string DocumentType,
    int Version,
    int ChunkIndex,
    float Score,
    string Snippet);
