namespace RagSystem.Application.Abstractions;

public record VectorPoint(
    string Id,
    float[] Vector,
    string DocumentId,
    string DocumentType,
    int Version,
    int ChunkIndex,
    string Text,
    IReadOnlyList<string> AllowedRoles);

public record SearchHit(
    string DocumentId,
    string DocumentType,
    int Version,
    int ChunkIndex,
    string Text,
    float Score,
    IReadOnlyList<string> AllowedRoles);

public interface IVectorStore
{
    Task EnsureCollectionAsync(int vectorSize, CancellationToken ct);
    Task UpsertAsync(IReadOnlyList<VectorPoint> points, CancellationToken ct);
    Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct);
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] queryVector,
        IReadOnlyList<string> userRoles,
        int topK,
        CancellationToken ct);
}
