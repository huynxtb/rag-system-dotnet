using RagSystem.Domain.Documents;

namespace RagSystem.Application.Abstractions;

public interface IDocumentTypeRepository
{
    Task<IReadOnlyList<DocumentType>> ListAsync(CancellationToken ct);
    Task<DocumentType?> GetByNameAsync(string name, CancellationToken ct);
    Task AddAsync(DocumentType type, CancellationToken ct);
}
