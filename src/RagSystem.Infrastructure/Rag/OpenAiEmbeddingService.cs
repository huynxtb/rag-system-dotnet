using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;
using RagSystem.Application.Abstractions;
using RagSystem.Infrastructure.Options;

namespace RagSystem.Infrastructure.Rag;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly OpenAiOptions _opts;

    public OpenAiEmbeddingService(IOptions<OpenAiOptions> opts)
    {
        _opts = opts.Value;
        _client = new OpenAIClient(_opts.ApiKey).GetEmbeddingClient(_opts.EmbeddingModel);
    }

    public int Dimension => _opts.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (texts.Count == 0) return Array.Empty<float[]>();
        var result = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
