using System.Security.Claims;
using RagSystem.Application.Abstractions;

namespace RagSystem.Api.Auth;

public class CurrentUser : ICurrentUser
{
    public string? Id { get; }
    public string? Email { get; }
    public IReadOnlyList<string> Roles { get; }
    public bool IsAuthenticated { get; }

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
        Id = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        Email = user?.FindFirstValue(ClaimTypes.Email);
        Roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                ?? new List<string>();
    }
}
