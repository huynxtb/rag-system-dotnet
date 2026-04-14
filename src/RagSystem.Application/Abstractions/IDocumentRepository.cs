using RagSystem.Domain.Documents;

namespace RagSystem.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document doc, CancellationToken ct);
    Task UpdateAsync(Document doc, CancellationToken ct);
    Task<Document?> GetByIdAsync(string id, CancellationToken ct);
    Task<Document?> GetLatestByTypeAndHashAsync(string type, string contentHash, CancellationToken ct);
    Task<int> GetMaxVersionByFileNameAndTypeAsync(string fileName, string type, CancellationToken ct);
    Task<IReadOnlyList<Document>> ListAsync(CancellationToken ct);
}
