using MongoDB.Driver;
using RagSystem.Application.Abstractions;
using RagSystem.Domain.Users;
using RagSystem.Infrastructure.Persistence.Documents;

namespace RagSystem.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly MongoContext _ctx;
    public UserRepository(MongoContext ctx) => _ctx = ctx;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var doc = await _ctx.Users.Find(u => u.Email == normalized).FirstOrDefaultAsync(ct);
        return doc is null ? null : Map(doc);
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct)
    {
        var doc = await _ctx.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : Map(doc);
    }

    public async Task UpsertAsync(User user, CancellationToken ct)
    {
        var doc = new UserDocument
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Roles = user.Roles.ToList()
        };
        await _ctx.Users.ReplaceOneAsync(
            u => u.Id == user.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken ct)
    {
        var roles = await _ctx.Users
            .Distinct<string>("Roles", FilterDefinition<UserDocument>.Empty)
            .ToListAsync(ct);
        return roles.OrderBy(r => r).ToList();
    }

    private static User Map(UserDocument d) =>
        new(d.Id, d.Email, d.PasswordHash, d.Roles);
}
