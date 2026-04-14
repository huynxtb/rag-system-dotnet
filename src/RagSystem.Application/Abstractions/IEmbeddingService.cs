namespace RagSystem.Application.Abstractions;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct);
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
    int Dimension { get; }
}
