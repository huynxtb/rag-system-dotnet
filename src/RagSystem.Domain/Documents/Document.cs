using RagSystem.Domain.Common;

namespace RagSystem.Domain.Documents;

/// <summary>
/// Aggregate root for an uploaded document. Versioning is monotonic per (type, file-key).
/// Access is controlled by AllowedRoles — at least one role must intersect with the user's roles.
/// </summary>
public class Document : Entity<string>
{
    public string FileName { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public int Version { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DocumentStatus Status { get; private set; }
    public IReadOnlyList<string> AllowedRoles => _allowedRoles;
    private readonly List<string> _allowedRoles = new();

    public string UploadedBy { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = default!;

    private Document() { }

    public Document(
        string id,
        string fileName,
        string type,
        int version,
        IEnumerable<string> allowedRoles,
        string uploadedBy,
        long sizeBytes,
        string contentHash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("FileName is required");
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Type is required");
        if (version < 1)
            throw new DomainException("Version must be >= 1");

        Id = id;
        FileName = fileName;
        Type = type.Trim().ToLowerInvariant();
        Version = version;
        UploadedAt = DateTime.UtcNow;
        Status = DocumentStatus.Pending;
        UploadedBy = uploadedBy;
        SizeBytes = sizeBytes;
        ContentHash = contentHash;

        var roles = allowedRoles.Select(r => r.Trim().ToLowerInvariant()).Distinct().ToList();
        if (roles.Count == 0)
            throw new DomainException("Document must have at least one allowed role");
        _allowedRoles.AddRange(roles);
    }

    public void MarkProcessing() => Status = DocumentStatus.Processing;
    public void MarkIndexed() => Status = DocumentStatus.Indexed;
    public void MarkFailed() => Status = DocumentStatus.Failed;

    /// <summary>Re-hydrate an aggregate from persistence without re-running invariants on timestamps/status.</summary>
    public static Document Rehydrate(
        string id, string fileName, string type, int version,
        IEnumerable<string> allowedRoles, string uploadedBy, long sizeBytes, string contentHash,
        DateTime uploadedAt, DocumentStatus status)
    {
        var d = new Document(id, fileName, type, version, allowedRoles, uploadedBy, sizeBytes, contentHash);
        d.UploadedAt = uploadedAt;
        d.Status = status;
        return d;
    }

    public bool CanBeReadBy(IEnumerable<string> userRoles)
    {
        var set = new HashSet<string>(userRoles, StringComparer.OrdinalIgnoreCase);
        return _allowedRoles.Any(r => set.Contains(r));
    }
}
