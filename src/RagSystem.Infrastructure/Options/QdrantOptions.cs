namespace RagSystem.Infrastructure.Options;

public class QdrantOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334; // gRPC
    public bool UseHttps { get; set; } = false;
    public string? ApiKey { get; set; }
    public string Collection { get; set; } = "rag_documents";
}
