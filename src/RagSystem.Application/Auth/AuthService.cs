using RagSystem.Application.Abstractions;

namespace RagSystem.Application.Auth;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email, IReadOnlyList<string> Roles);

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct);
        if (user is null) return null;
        if (!_hasher.Verify(req.Password, user.PasswordHash)) return null;

        var token = _jwt.CreateToken(user);
        return new LoginResponse(token, user.Email, user.Roles);
    }
}
