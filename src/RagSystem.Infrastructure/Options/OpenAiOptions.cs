namespace RagSystem.Infrastructure.Options;

public class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small"; // 1536 dims
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public int EmbeddingDimensions { get; set; } = 1536;
}
