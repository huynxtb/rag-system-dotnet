using Microsoft.Extensions.DependencyInjection;
using RagSystem.Application.Auth;
using RagSystem.Application.Chats;
using RagSystem.Application.Documents;
using RagSystem.Application.Rag;

namespace RagSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<DocumentTypeService>();
        services.AddScoped<RagService>();
        services.AddScoped<ChatSessionService>();
        return services;
    }
}
