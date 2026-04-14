using RagSystem.Domain.Common;

namespace RagSystem.Domain.Documents;

/// <summary>
/// Built-in & custom document categories. Built-ins per spec:
/// "tài chính", "chính sách bảo mật", "khác".
/// </summary>
public class DocumentType : Entity<string>
{
    public string Name { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool IsBuiltIn { get; private set; }

    private DocumentType() { }

    public DocumentType(string id, string name, string displayName, bool isBuiltIn = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Type name is required");
        Id = id;
        Name = name.Trim().ToLowerInvariant();
        DisplayName = displayName;
        IsBuiltIn = isBuiltIn;
    }

    public static class BuiltIn
    {
        public const string Finance = "finance";
        public const string SecurityPolicy = "security_policy";
        public const string Other = "other";
    }
}
