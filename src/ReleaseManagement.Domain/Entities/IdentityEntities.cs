using ReleaseManagement.Domain.Common;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    private ApplicationUser()
    {
    }

    public ApplicationUser(
        Guid id,
        string userName,
        string fullName,
        string email,
        DateTime createdDateUtc)
        : base(id, createdDateUtc)
    {
        UserName = Required(userName, nameof(userName));
        FullName = Required(fullName, nameof(fullName));
        Email = Required(email, nameof(email));
        IsActive = true;
    }

    public string? ExternalId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Department { get; private set; }

    public string? Position { get; private set; }

    public bool IsActive { get; private set; }

    public void SetExternalIdentity(string externalId, DateTime updatedDateUtc)
    {
        ExternalId = Required(externalId, nameof(externalId));
        MarkUpdated(updatedDateUtc);
    }

    public void Deactivate(DateTime updatedDateUtc)
    {
        IsActive = false;
        MarkUpdated(updatedDateUtc);
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class Role : Entity
{
    private Role()
    {
    }

    public Role(Guid id, string name, string? description)
        : base(id)
    {
        Name = Required(name, nameof(name));
        Description = description?.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = RequiredId(userId, nameof(userId));
        RoleId = RequiredId(roleId, nameof(roleId));
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return value;
    }
}

public sealed class UserProductAccess
{
    private UserProductAccess()
    {
    }

    public UserProductAccess(Guid userId, Guid productId, ProductAccessType accessType)
    {
        UserId = RequiredId(userId, nameof(userId));
        ProductId = RequiredId(productId, nameof(productId));
        AccessType = accessType;
    }

    public Guid UserId { get; private set; }

    public Guid ProductId { get; private set; }

    public ProductAccessType AccessType { get; private set; }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return value;
    }
}
