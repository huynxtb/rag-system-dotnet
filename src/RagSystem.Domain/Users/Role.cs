namespace RagSystem.Domain.Users;

/// <summary>
/// Built-in role names. Roles are stored as plain strings on the User aggregate so
/// new roles (e.g. coming from custom document types) can be assigned without
/// changing the domain model.
/// </summary>
public static class Role
{
    public const string Admin = "admin";
    public const string User = "user";
}
