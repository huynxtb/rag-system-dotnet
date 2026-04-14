using RagSystem.Domain.Users;

namespace RagSystem.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
