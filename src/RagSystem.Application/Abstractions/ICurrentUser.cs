namespace RagSystem.Application.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
