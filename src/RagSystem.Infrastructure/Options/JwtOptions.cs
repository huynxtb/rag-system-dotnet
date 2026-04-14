namespace RagSystem.Infrastructure.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "ragsystem";
    public string Audience { get; set; } = "ragsystem-clients";
    /// <summary>HMAC signing key — must be at least 32 bytes for HS256.</summary>
    public string SigningKey { get; set; } = "dev-only-signing-key-please-change-me-32b!";
    public int ExpiryMinutes { get; set; } = 480;
}
