using MongoDB.Driver;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Chats;
using RagSystem.Infrastructure.Persistence.Documents;

namespace RagSystem.Infrastructure.Persistence;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly MongoContext _ctx;
    public ChatSessionRepository(MongoContext ctx) => _ctx = ctx;

    public async Task<ChatSession?> GetByIdAsync(string id, CancellationToken ct)
    {
        var d = await _ctx.ChatSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return d is null ? null : Hydrate(d);
    }

    public async Task<IReadOnlyList<ChatSession>> ListByUserAsync(string userId, CancellationToken ct)
    {
        var docs = await _ctx.ChatSessions
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);
        return docs.Select(Hydrate).ToList();
    }

    public async Task UpsertAsync(ChatSession session, CancellationToken ct) =>
        await _ctx.ChatSessions.ReplaceOneAsync(
            x => x.Id == session.Id,
            Map(session),
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task DeleteAsync(string id, CancellationToken ct) =>
        await _ctx.ChatSessions.DeleteOneAsync(x => x.Id == id, ct);

    private static ChatSessionDocument Map(ChatSession s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        Title = s.Title,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        Messages = s.Messages.Select(m => new ChatMessageDocument
        {
            Role = m.Role.ToString(),
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            Sources = m.Sources.Select(sr => new ChatSourceDocument
            {
                DocumentId = sr.DocumentId,
                DocumentType = sr.DocumentType,
                Version = sr.Version,
                ChunkIndex = sr.ChunkIndex,
                Score = sr.Score,
                Snippet = sr.Snippet
            }).ToList()
        }).ToList()
    };

    private static ChatSession Hydrate(ChatSessionDocument d)
    {
        var messages = d.Messages.Select(m => new ChatMessage(
            Enum.TryParse<ChatRole>(m.Role, out var role) ? role : ChatRole.User,
            m.Content,
            m.CreatedAt,
            m.Sources.Select(sr => new ChatSourceRef(
                sr.DocumentId, sr.DocumentType, sr.Version, sr.ChunkIndex, sr.Score, sr.Snippet)).ToList()
        )).ToList();

        return ChatSession.Rehydrate(d.Id, d.UserId, d.Title, d.CreatedAt, d.UpdatedAt, messages);
    }
}
