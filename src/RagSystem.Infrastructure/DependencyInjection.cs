using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RagSystem.Application.Abstractions;
using RagSystem.Infrastructure.Auth;
using RagSystem.Infrastructure.Options;
using RagSystem.Infrastructure.Persistence;
using RagSystem.Infrastructure.Rag;
using RagSystem.Infrastructure.Seeding;

namespace RagSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoOptions>(config.GetSection("Mongo"));
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.Configure<OpenAiOptions>(config.GetSection("OpenAi"));
        services.Configure<QdrantOptions>(config.GetSection("Qdrant"));

        services.AddSingleton<MongoContext>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<IChatCompletionService, OpenAiChatCompletionService>();
        services.AddSingleton<IVectorStore, QdrantVectorStore>();

        services.AddSingleton<ITextChunker>(_ => new PlainTextChunker(chunkSize: 1000, overlap: 150));
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

        services.AddHostedService<DataSeeder>();
        return services;
    }
}
