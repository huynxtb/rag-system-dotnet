using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Common;
using RagSystem.Domain.Documents;

namespace RagSystem.Application.Documents;

public record UploadDocumentRequest(
    Stream Content,
    string FileName,
    string Type,
    IReadOnlyList<string> AllowedRoles);

public record DocumentDto(
    string Id,
    string FileName,
    string Type,
    int Version,
    DateTime UploadedAt,
    string Status,
    IReadOnlyList<string> AllowedRoles);

public class DocumentService
{
    private readonly IDocumentRepository _docs;
    private readonly IDocumentTypeRepository _types;
    private readonly IDocumentTextExtractor _extractor;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository docs,
        IDocumentTypeRepository types,
        IDocumentTextExtractor extractor,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore vectors,
        ICurrentUser currentUser,
        ILogger<DocumentService> logger)
    {
        _docs = docs;
        _types = types;
        _extractor = extractor;
        _chunker = chunker;
        _embeddings = embeddings;
        _vectors = vectors;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new DomainException("Authentication required");

        var type = await _types.GetByNameAsync(req.Type, ct)
            ?? throw new DomainException($"Unknown document type: {req.Type}");

        // Materialize content + hash to enable dedup across versions.
        using var ms = new MemoryStream();
        await req.Content.CopyToAsync(ms, ct);
        ms.Position = 0;
        var contentHash = Convert.ToHexString(SHA256.HashData(ms.ToArray()));
        ms.Position = 0;

        // Per-spec: re-chunk/re-embed and bump version when same (fileName, type) is re-uploaded.
        var maxVersion = await _docs.GetMaxVersionByFileNameAndTypeAsync(req.FileName, type.Name, ct);
        var nextVersion = maxVersion + 1;

        var doc = new Document(
            id: Guid.NewGuid().ToString("N"),
            fileName: req.FileName,
            type: type.Name,
            version: nextVersion,
            allowedRoles: req.AllowedRoles,
            uploadedBy: _currentUser.Id ?? "unknown",
            sizeBytes: ms.Length,
            contentHash: contentHash);

        await _docs.AddAsync(doc, ct);

        try
        {
            doc.MarkProcessing();
            await _docs.UpdateAsync(doc, ct);

            ms.Position = 0;
            var text = await _extractor.ExtractAsync(ms, req.FileName, ct);
            if (string.IsNullOrWhiteSpace(text))
                throw new DomainException("Extracted document text is empty");

            var chunks = _chunker.Chunk(text);
            _logger.LogInformation("Document {DocId} chunked into {Count} chunks", doc.Id, chunks.Count);

            var embeddings = await _embeddings.EmbedBatchAsync(chunks, ct);
            await _vectors.EnsureCollectionAsync(_embeddings.Dimension, ct);

            var points = new List<VectorPoint>(chunks.Count);
            for (int i = 0; i < chunks.Count; i++)
            {
                points.Add(new VectorPoint(
                    Id: $"{doc.Id}:{i}",
                    Vector: embeddings[i],
                    DocumentId: doc.Id,
                    DocumentType: doc.Type,
                    Version: doc.Version,
                    ChunkIndex: i,
                    Text: chunks[i],
                    AllowedRoles: doc.AllowedRoles));
            }
            await _vectors.UpsertAsync(points, ct);

            doc.MarkIndexed();
            await _docs.UpdateAsync(doc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocId}", doc.Id);
            doc.MarkFailed();
            await _docs.UpdateAsync(doc, ct);
            throw;
        }

        return ToDto(doc);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListVisibleAsync(CancellationToken ct)
    {
        var all = await _docs.ListAsync(ct);
        var roles = _currentUser.Roles;
        return all.Where(d => d.CanBeReadBy(roles)).Select(ToDto).ToList();
    }

    public async Task<DocumentDto?> GetAsync(string id, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(id, ct);
        if (doc is null) return null;
        if (!doc.CanBeReadBy(_currentUser.Roles))
            throw new ForbiddenAccessException(doc.AllowedRoles);
        return ToDto(doc);
    }

    private static DocumentDto ToDto(Document d) => new(
        d.Id, d.FileName, d.Type, d.Version, d.UploadedAt, d.Status.ToString(), d.AllowedRoles);
}

public class ForbiddenAccessException : Exception
{
    public IReadOnlyList<string> RequiredRoles { get; }
    public ForbiddenAccessException(IReadOnlyList<string> requiredRoles)
        : base($"Access denied. Required role(s): {string.Join(", ", requiredRoles)}")
    {
        RequiredRoles = requiredRoles;
    }
}
