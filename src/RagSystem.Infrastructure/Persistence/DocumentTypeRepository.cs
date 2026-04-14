using MongoDB.Driver;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Documents;
using RagSystem.Infrastructure.Persistence.Documents;

namespace RagSystem.Infrastructure.Persistence;

public class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly MongoContext _ctx;
    public DocumentTypeRepository(MongoContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<DocumentType>> ListAsync(CancellationToken ct)
    {
        var docs = await _ctx.DocumentTypes.Find(FilterDefinition<DocumentTypeDocument>.Empty)
            .SortBy(t => t.DisplayName).ToListAsync(ct);
        return docs.Select(Map).ToList();
    }

    public async Task<DocumentType?> GetByNameAsync(string name, CancellationToken ct)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var doc = await _ctx.DocumentTypes.Find(t => t.Name == normalized).FirstOrDefaultAsync(ct);
        return doc is null ? null : Map(doc);
    }

    public async Task AddAsync(DocumentType type, CancellationToken ct) =>
        await _ctx.DocumentTypes.InsertOneAsync(new DocumentTypeDocument
        {
            Id = type.Id,
            Name = type.Name,
            DisplayName = type.DisplayName,
            IsBuiltIn = type.IsBuiltIn
        }, cancellationToken: ct);

    private static DocumentType Map(DocumentTypeDocument d) =>
        new(d.Id, d.Name, d.DisplayName, d.IsBuiltIn);
}
