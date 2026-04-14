using System.Text;
using DocumentFormat.OpenXml.Packaging;
using RagSystem.Application.Abstractions;
using UglyToad.PdfPig;

namespace RagSystem.Infrastructure.Rag;

public class DocumentTextExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            _ => await ExtractTextAsync(stream, ct)
        };
    }

    private static async Task<string> ExtractTextAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }

    private static string ExtractPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(stream);
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        return body?.InnerText ?? string.Empty;
    }
}
