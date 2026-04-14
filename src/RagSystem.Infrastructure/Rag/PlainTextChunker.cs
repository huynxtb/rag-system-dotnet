using RagSystem.Application.Abstractions;

namespace RagSystem.Infrastructure.Rag;

/// <summary>
/// Simple character-window chunker with overlap. Adequate for demo;
/// production pipelines should prefer a sentence-aware chunker.
/// </summary>
public class PlainTextChunker : ITextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public PlainTextChunker(int chunkSize = 1000, int overlap = 150)
    {
        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public IReadOnlyList<string> Chunk(string text)
    {
        text = text.Replace("\r\n", "\n").Trim();
        if (text.Length == 0) return Array.Empty<string>();
        if (text.Length <= _chunkSize) return new[] { text };

        var chunks = new List<string>();
        var step = _chunkSize - _overlap;
        for (int start = 0; start < text.Length; start += step)
        {
            var end = Math.Min(start + _chunkSize, text.Length);
            chunks.Add(text.Substring(start, end - start));
            if (end == text.Length) break;
        }
        return chunks;
    }
}
