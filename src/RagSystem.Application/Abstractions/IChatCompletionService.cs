namespace RagSystem.Application.Abstractions;

public record ChatTurn(string Role, string Content);

public interface IChatCompletionService
{
    Task<string> CompleteAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct);
}
