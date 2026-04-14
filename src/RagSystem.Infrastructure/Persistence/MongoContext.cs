using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using RagSystem.Infrastructure.Options;
using RagSystem.Infrastructure.Persistence.Documents;

namespace RagSystem.Infrastructure.Persistence;

public class MongoContext
{
    public IMongoDatabase Db { get; }

    public IMongoCollection<UserDocument> Users => Db.GetCollection<UserDocument>("users");
    public IMongoCollection<DocumentDocument> Documents => Db.GetCollection<DocumentDocument>("documents");
    public IMongoCollection<DocumentTypeDocument> DocumentTypes => Db.GetCollection<DocumentTypeDocument>("document_types");
    public IMongoCollection<ChatSessionDocument> ChatSessions => Db.GetCollection<ChatSessionDocument>("chat_sessions");

    static MongoContext()
    {
        var pack = new ConventionPack { new IgnoreExtraElementsConvention(true), new EnumRepresentationConvention(BsonType.String) };
        ConventionRegistry.Register("RagConventions", pack, _ => true);
    }

    public MongoContext(IOptions<MongoOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        Db = client.GetDatabase(options.Value.Database);
    }
}
