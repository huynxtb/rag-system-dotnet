using MongoDB.Driver;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Documents;
using RagSystem.Infrastructure.Persistence.Documents;

namespace RagSystem.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly MongoContext _ctx;
    public DocumentRepository(MongoContext ctx) => _ctx = ctx;

    public async Task AddAsync(Document doc, CancellationToken ct) =>
        await _ctx.Documents.InsertOneAsync(Map(doc), cancellationToken: ct);

    public async Task UpdateAsync(Document doc, CancellationToken ct) =>
        await _ctx.Documents.ReplaceOneAsync(d => d.Id == doc.Id, Map(doc), cancellationToken: ct);

    public async Task<Document?> GetByIdAsync(string id, CancellationToken ct)
    {
        var d = await _ctx.Documents.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return d is null ? null : Hydrate(d);
    }

    public async Task<Document?> GetLatestByTypeAndHashAsync(string type, string contentHash, CancellationToken ct)
    {
        var d = await _ctx.Documents
            .Find(x => x.Type == type && x.ContentHash == contentHash)
            .SortByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);
        return d is null ? null : Hydrate(d);
    }

    public async Task<int> GetMaxVersionByFileNameAndTypeAsync(string fileName, string type, CancellationToken ct)
    {
        var d = await _ctx.Documents
            .Find(x => x.FileName == fileName && x.Type == type)
            .SortByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);
        return d?.Version ?? 0;
    }

    public async Task<IReadOnlyList<Document>> ListAsync(CancellationToken ct)
    {
        var docs = await _ctx.Documents.Find(FilterDefinition<DocumentDocument>.Empty)
            .SortByDescending(d => d.UploadedAt).ToListAsync(ct);
        return docs.Select(Hydrate).ToList();
    }

    private static DocumentDocument Map(Document d) => new()
    {
        Id = d.Id,
        FileName = d.FileName,
        Type = d.Type,
        Version = d.Version,
        UploadedAt = d.UploadedAt,
        Status = d.Status.ToString(),
        AllowedRoles = d.AllowedRoles.ToList(),
        UploadedBy = d.UploadedBy,
        SizeBytes = d.SizeBytes,
        ContentHash = d.ContentHash
    };

    private static Document Hydrate(DocumentDocument d)
    {
        var status = Enum.TryParse<DocumentStatus>(d.Status, out var s) ? s : DocumentStatus.Pending;
        return Document.Rehydrate(
            d.Id, d.FileName, d.Type, d.Version, d.AllowedRoles,
            d.UploadedBy, d.SizeBytes, d.ContentHash, d.UploadedAt, status);
    }
}
