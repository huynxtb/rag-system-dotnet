using RagSystem.Application.Abstractions;
using RagSystem.Domain.Common;
using RagSystem.Domain.Documents;

namespace RagSystem.Application.Documents;

public record DocumentTypeDto(string Id, string Name, string DisplayName, bool IsBuiltIn);

public class DocumentTypeService
{
    private readonly IDocumentTypeRepository _repo;

    public DocumentTypeService(IDocumentTypeRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DocumentTypeDto>> ListAsync(CancellationToken ct)
    {
        var items = await _repo.ListAsync(ct);
        return items.Select(t => new DocumentTypeDto(t.Id, t.Name, t.DisplayName, t.IsBuiltIn)).ToList();
    }

    public async Task<DocumentTypeDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var existing = await _repo.GetByNameAsync(name, ct);
        if (existing is not null)
            throw new DomainException("Document type already exists");

        var entity = new DocumentType(Guid.NewGuid().ToString("N"), name, displayName, isBuiltIn: false);
        await _repo.AddAsync(entity, ct);
        return new DocumentTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.IsBuiltIn);
    }
}
