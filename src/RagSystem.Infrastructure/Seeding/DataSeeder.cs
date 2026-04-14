using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Documents;
using RagSystem.Domain.Users;

namespace RagSystem.Infrastructure.Seeding;

/// <summary>
/// Hosted service that runs once at startup to ensure the test accounts and
/// built-in document types described in DOCX.md exist.
/// </summary>
public class DataSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IServiceScopeFactory scopeFactory, ILogger<DataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var types = scope.ServiceProvider.GetRequiredService<IDocumentTypeRepository>();

            // Built-in document types
            await EnsureType(types, DocumentType.BuiltIn.Finance, "Tài chính", ct);
            await EnsureType(types, DocumentType.BuiltIn.SecurityPolicy, "Chính sách bảo mật", ct);
            await EnsureType(types, DocumentType.BuiltIn.Other, "Khác", ct);

            // Test accounts
            const string demoPassword = "12345678Aa";
            await EnsureUser(users, hasher,
                "admin@gmail.com", demoPassword, new[] { Role.Admin, Role.User }, ct);
            await EnsureUser(users, hasher,
                "user@gmail.com", demoPassword, new[] { Role.User }, ct);

            _logger.LogInformation("Seeding complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seeding failed (will continue on next start). Is MongoDB reachable?");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task EnsureType(IDocumentTypeRepository repo, string name, string display, CancellationToken ct)
    {
        var existing = await repo.GetByNameAsync(name, ct);
        if (existing is not null) return;
        await repo.AddAsync(new DocumentType(Guid.NewGuid().ToString("N"), name, display, isBuiltIn: true), ct);
    }

    private static async Task EnsureUser(
        IUserRepository repo, IPasswordHasher hasher,
        string email, string password, string[] roles, CancellationToken ct)
    {
        var existing = await repo.GetByEmailAsync(email, ct);
        if (existing is not null) return;
        var user = new User(Guid.NewGuid().ToString("N"), email, hasher.Hash(password), roles);
        await repo.UpsertAsync(user, ct);
    }
}
