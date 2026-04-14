using RagSystem.Domain.Common;

namespace RagSystem.Domain.Users;

public class User : Entity<string>
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public IReadOnlyList<string> Roles => _roles;
    private readonly List<string> _roles = new();

    private User() { }

    public User(string id, string email, string passwordHash, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required");

        Id = id;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        _roles.AddRange(roles.Select(r => r.Trim().ToLowerInvariant()).Distinct());
        if (_roles.Count == 0)
            throw new DomainException("User must have at least one role");
    }

    public bool HasAnyRole(IEnumerable<string> required)
    {
        var set = new HashSet<string>(_roles, StringComparer.OrdinalIgnoreCase);
        return required.Any(r => set.Contains(r));
    }
}
