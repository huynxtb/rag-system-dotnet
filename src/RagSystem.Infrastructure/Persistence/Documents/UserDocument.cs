using MongoDB.Bson.Serialization.Attributes;

namespace RagSystem.Infrastructure.Persistence.Documents;

public class UserDocument
{
    [BsonId] public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
}
