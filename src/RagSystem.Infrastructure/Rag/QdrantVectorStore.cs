using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagSystem.Application.Abstractions;
using RagSystem.Infrastructure.Options;

namespace RagSystem.Infrastructure.Rag;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _opts;
    private readonly ILogger<QdrantVectorStore> _logger;
    private bool _ensured;

    public QdrantVectorStore(IOptions<QdrantOptions> opts, ILogger<QdrantVectorStore> logger)
    {
        _opts = opts.Value;
        _logger = logger;
        _client = new QdrantClient(_opts.Host, _opts.Port, https: _opts.UseHttps, apiKey: _opts.ApiKey);
    }

    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken ct)
    {
        if (_ensured) return;
        var exists = await _client.CollectionExistsAsync(_opts.Collection, ct);
        if (!exists)
        {
            await _client.CreateCollectionAsync(
                _opts.Collection,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine },
                cancellationToken: ct);
            _logger.LogInformation("Created Qdrant collection {Collection} (dim={Dim})", _opts.Collection, vectorSize);

            // Index allowed_roles for filtered search.
            await _client.CreatePayloadIndexAsync(
                _opts.Collection,
                "allowed_roles",
                schemaType: PayloadSchemaType.Keyword,
                cancellationToken: ct);
            await _client.CreatePayloadIndexAsync(
                _opts.Collection,
                "document_id",
                schemaType: PayloadSchemaType.Keyword,
                cancellationToken: ct);
        }
        _ensured = true;
    }

    public async Task UpsertAsync(IReadOnlyList<VectorPoint> points, CancellationToken ct)
    {
        if (points.Count == 0) return;
        var qPoints = points.Select(p =>
        {
            var pt = new PointStruct
            {
                // Use deterministic UUIDv5-ish via GUID hashing of the string id.
                Id = new PointId { Uuid = DeterministicGuid(p.Id).ToString() },
                Vectors = p.Vector
            };
            pt.Payload["document_id"] = p.DocumentId;
            pt.Payload["document_type"] = p.DocumentType;
            pt.Payload["version"] = p.Version;
            pt.Payload["chunk_index"] = p.ChunkIndex;
            pt.Payload["text"] = p.Text;
            pt.Payload["allowed_roles"] = new Value
            {
                ListValue = new ListValue { Values = { p.AllowedRoles.Select(r => new Value { StringValue = r }) } }
            };
            return pt;
        }).ToList();

        await _client.UpsertAsync(_opts.Collection, qPoints, cancellationToken: ct);
    }

    public async Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct)
    {
        var filter = new Filter
        {
            Must = { new Condition { Field = new FieldCondition { Key = "document_id", Match = new Match { Keyword = documentId } } } }
        };
        await _client.DeleteAsync(_opts.Collection, filter, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        float[] queryVector,
        IReadOnlyList<string> userRoles,
        int topK,
        CancellationToken ct)
    {
        var filter = new Filter();
        if (userRoles.Count > 0)
        {
            // Match if any of user's roles overlap with allowed_roles.
            var roleConditions = userRoles.Select(r => new Condition
            {
                Field = new FieldCondition { Key = "allowed_roles", Match = new Match { Keyword = r } }
            });
            filter.Should.AddRange(roleConditions);
        }
        else
        {
            // No roles → no documents accessible.
            return Array.Empty<SearchHit>();
        }

        var results = await _client.SearchAsync(
            _opts.Collection,
            queryVector,
            filter: filter,
            limit: (ulong)topK,
            cancellationToken: ct);

        var hits = new List<SearchHit>(results.Count);
        foreach (var p in results)
        {
            var payload = p.Payload;
            var roles = payload.TryGetValue("allowed_roles", out var rv) && rv.KindCase == Value.KindOneofCase.ListValue
                ? rv.ListValue.Values.Select(v => v.StringValue).ToList()
                : new List<string>();

            hits.Add(new SearchHit(
                DocumentId: payload.GetValueOrDefault("document_id")?.StringValue ?? "",
                DocumentType: payload.GetValueOrDefault("document_type")?.StringValue ?? "",
                Version: (int)(payload.GetValueOrDefault("version")?.IntegerValue ?? 0),
                ChunkIndex: (int)(payload.GetValueOrDefault("chunk_index")?.IntegerValue ?? 0),
                Text: payload.GetValueOrDefault("text")?.StringValue ?? "",
                Score: p.Score,
                AllowedRoles: roles));
        }
        return hits;
    }

    private static Guid DeterministicGuid(string input)
    {
        using var sha = System.Security.Cryptography.SHA1.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        Array.Resize(ref bytes, 16);
        return new Guid(bytes);
    }
}
