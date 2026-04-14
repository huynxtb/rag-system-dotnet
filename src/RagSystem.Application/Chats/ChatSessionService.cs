using RagSystem.Application.Abstractions;
using RagSystem.Domain.Chats;
using RagSystem.Domain.Common;

namespace RagSystem.Application.Chats;

public record ChatMessageDto(
    string Role,
    string Content,
    DateTime CreatedAt,
    IReadOnlyList<ChatSourceDto> Sources);

public record ChatSourceDto(
    string DocumentId,
    string DocumentType,
    int Version,
    int ChunkIndex,
    float Score,
    string Snippet);

public record ChatSessionSummaryDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MessageCount);

public record ChatSessionDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ChatMessageDto> Messages);

public class ChatSessionService
{
    private readonly IChatSessionRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ChatSessionService(IChatSessionRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ChatSessionSummaryDto>> ListMineAsync(CancellationToken ct)
    {
        RequireAuth();
        var items = await _repo.ListByUserAsync(_currentUser.Id!, ct);
        return items
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new ChatSessionSummaryDto(s.Id, s.Title, s.CreatedAt, s.UpdatedAt, s.Messages.Count))
            .ToList();
    }

    public async Task<ChatSessionDto> CreateAsync(CancellationToken ct)
    {
        RequireAuth();
        var session = new ChatSession(Guid.NewGuid().ToString("N"), _currentUser.Id!);
        await _repo.UpsertAsync(session, ct);
        return ToDto(session);
    }

    public async Task<ChatSessionDto?> GetAsync(string id, CancellationToken ct)
    {
        RequireAuth();
        var s = await _repo.GetByIdAsync(id, ct);
        if (s is null) return null;
        if (s.UserId != _currentUser.Id) throw new DomainException("Not your session");
        return ToDto(s);
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        RequireAuth();
        var s = await _repo.GetByIdAsync(id, ct);
        if (s is null) return;
        if (s.UserId != _currentUser.Id) throw new DomainException("Not your session");
        await _repo.DeleteAsync(id, ct);
    }

    private void RequireAuth()
    {
        if (!_currentUser.IsAuthenticated) throw new DomainException("Authentication required");
    }

    internal static ChatSessionDto ToDto(ChatSession s) => new(
        s.Id, s.Title, s.CreatedAt, s.UpdatedAt,
        s.Messages.Select(m => new ChatMessageDto(
            m.Role.ToString(),
            m.Content,
            m.CreatedAt,
            m.Sources.Select(sr => new ChatSourceDto(
                sr.DocumentId, sr.DocumentType, sr.Version, sr.ChunkIndex, sr.Score, sr.Snippet)).ToList()
        )).ToList());
}
