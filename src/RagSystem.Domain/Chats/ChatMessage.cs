namespace RagSystem.Domain.Chats;

public record ChatMessage(
    ChatRole Role,
    string Content,
    DateTime CreatedAt,
    IReadOnlyList<ChatSourceRef> Sources);
