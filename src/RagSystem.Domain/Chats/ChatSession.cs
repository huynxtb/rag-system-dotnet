using RagSystem.Domain.Common;

namespace RagSystem.Domain.Chats;

/// <summary>
/// Aggregate root for a user's chat conversation. Messages are appended in order
/// and the title is auto-derived from the first user message (may be overridden).
/// </summary>
public class ChatSession : Entity<string>
{
    public string UserId { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages;
    private readonly List<ChatMessage> _messages = new();

    private ChatSession() { }

    public ChatSession(string id, string userId, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId is required");
        Id = id;
        UserId = userId;
        Title = title is null || string.IsNullOrWhiteSpace(title) ? "New chat" : title.Trim();
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }

    public void AppendUserMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Message content is required");
        _messages.Add(new ChatMessage(ChatRole.User, content.Trim(), DateTime.UtcNow, Array.Empty<ChatSourceRef>()));
        UpdatedAt = DateTime.UtcNow;
        // Auto-title from the first user message if still default.
        if (Title == "New chat" && _messages.Count(m => m.Role == ChatRole.User) == 1)
            Title = Truncate(content.Trim(), 60);
    }

    public void AppendAssistantMessage(string content, IReadOnlyList<ChatSourceRef> sources)
    {
        _messages.Add(new ChatMessage(ChatRole.Assistant, content, DateTime.UtcNow, sources));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required");
        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public static ChatSession Rehydrate(
        string id, string userId, string title,
        DateTime createdAt, DateTime updatedAt,
        IEnumerable<ChatMessage> messages)
    {
        var s = new ChatSession(id, userId, title);
        s.CreatedAt = createdAt;
        s.UpdatedAt = updatedAt;
        s._messages.Clear();
        s._messages.AddRange(messages);
        return s;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";
}
