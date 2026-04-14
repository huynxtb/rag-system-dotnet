using RagSystem.Domain.Users;

namespace RagSystem.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(string id, CancellationToken ct);
    Task UpsertAsync(User user, CancellationToken ct);
    Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken ct);
}
