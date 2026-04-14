using RagSystem.Domain.Chats;

namespace RagSystem.Application.Abstractions;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<ChatSession>> ListByUserAsync(string userId, CancellationToken ct);
    Task UpsertAsync(ChatSession session, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
}
