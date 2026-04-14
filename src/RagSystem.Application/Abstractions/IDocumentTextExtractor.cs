namespace RagSystem.Application.Abstractions;

public interface IDocumentTextExtractor
{
    /// <summary>Extracts plain text from a document stream. Supports .txt, .md, .pdf, .docx.</summary>
    Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken ct);
}
