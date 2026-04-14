using RagSystem.Application.Abstractions;
using RagSystem.Application.Chats;
using RagSystem.Domain.Chats;
using RagSystem.Domain.Common;

namespace RagSystem.Application.Rag;

public record AskRequest(string Question, int TopK = 5);
public record AskInSessionRequest(string Question, int TopK = 5);
public record SourceDto(string DocumentId, int ChunkIndex, int Version, string DocumentType, float Score, string Snippet);
public record AskResponse(string Answer, IReadOnlyList<SourceDto> Sources);

public class RagService
{
    private const int MaxHistoryTurnsForContext = 10;

    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly IChatCompletionService _chat;
    private readonly ICurrentUser _currentUser;
    private readonly IChatSessionRepository _sessions;

    public RagService(
        IEmbeddingService embeddings,
        IVectorStore vectors,
        IChatCompletionService chat,
        ICurrentUser currentUser,
        IChatSessionRepository sessions)
    {
        _embeddings = embeddings;
        _vectors = vectors;
        _chat = chat;
        _currentUser = currentUser;
        _sessions = sessions;
    }

    /// <summary>Stateless single-shot Q&A — no persistence.</summary>
    public async Task<AskResponse> AskAsync(AskRequest req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new DomainException("Authentication required");
        if (string.IsNullOrWhiteSpace(req.Question))
            throw new DomainException("Question is required");

        var (answer, sources) = await RunRagAsync(req.Question, history: null, Math.Clamp(req.TopK, 1, 20), ct);
        return new AskResponse(answer, sources.Select(ToDto).ToList());
    }

    /// <summary>Session-aware Q&A — loads prior turns, appends user+assistant, persists.</summary>
    public async Task<ChatSessionDto> AskInSessionAsync(string sessionId, AskInSessionRequest req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new DomainException("Authentication required");
        if (string.IsNullOrWhiteSpace(req.Question))
            throw new DomainException("Question is required");

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new DomainException("Session not found");
        if (session.UserId != _currentUser.Id)
            throw new DomainException("Not your session");

        // Snapshot history BEFORE appending the new user message, so RunRag sees prior turns only.
        var history = session.Messages.TakeLast(MaxHistoryTurnsForContext * 2).ToList();
        session.AppendUserMessage(req.Question);

        var (answer, hits) = await RunRagAsync(req.Question, history, Math.Clamp(req.TopK, 1, 20), ct);

        var sources = hits.Select(h => new ChatSourceRef(
            h.DocumentId, h.DocumentType, h.Version, h.ChunkIndex, h.Score,
            h.Text.Length > 240 ? h.Text[..240] + "…" : h.Text)).ToList();
        session.AppendAssistantMessage(answer, sources);

        await _sessions.UpsertAsync(session, ct);
        return ChatSessionService.ToDto(session);
    }

    private async Task<(string answer, IReadOnlyList<SearchHit> hits)> RunRagAsync(
        string question,
        IReadOnlyList<ChatMessage>? history,
        int topK,
        CancellationToken ct)
    {
        var queryVec = await _embeddings.EmbedAsync(question, ct);
        var hits = await _vectors.SearchAsync(queryVec, _currentUser.Roles, topK, ct);

        if (hits.Count == 0 && (history is null || history.Count == 0))
        {
            return (
                "I couldn't find any documents you have access to that contain information about that.",
                Array.Empty<SearchHit>());
        }

        var contextBlock = hits.Count == 0
            ? "(No new document excerpts retrieved — rely on the conversation history.)"
            : string.Join("\n\n---\n\n",
                hits.Select((h, i) => $"[Source {i + 1} | doc={h.DocumentId} v{h.Version} chunk={h.ChunkIndex}]\n{h.Text}"));

        var systemPrompt =
            "You are a helpful enterprise assistant. Answer using the provided document excerpts and " +
            "the ongoing conversation. If the answer is not grounded in the excerpts, say you don't know. " +
            "Cite excerpts by [Source N]. Keep answers concise.";

        var messages = new List<ChatTurn>
        {
            new("system", systemPrompt),
            new("system", $"Document excerpts for this turn:\n{contextBlock}")
        };
        if (history is not null)
        {
            foreach (var m in history)
                messages.Add(new ChatTurn(m.Role == ChatRole.User ? "user" : "assistant", m.Content));
        }
        messages.Add(new ChatTurn("user", question));

        var answer = await _chat.CompleteAsync(messages, ct);
        return (answer, hits);
    }

    private static SourceDto ToDto(SearchHit h) =>
        new(h.DocumentId, h.ChunkIndex, h.Version, h.DocumentType, h.Score,
            h.Text.Length > 240 ? h.Text[..240] + "…" : h.Text);
}
