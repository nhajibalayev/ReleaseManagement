using ReleaseManagement.Domain.Common;

namespace ReleaseManagement.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product()
    {
    }

    public Product(Guid id, string name, string code, string? description, DateTime createdDateUtc)
        : base(id, createdDateUtc)
    {
        Name = Required(name, nameof(name));
        Code = Required(code, nameof(code));
        Description = description?.Trim();
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

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

public sealed class Service : Entity
{
    private Service()
    {
    }

    public Service(
        Guid id,
        Guid productId,
        string name,
        string code,
        string? repositoryUrl,
        string? azureDevOpsProject,
        Guid? azureDevOpsRepositoryId)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product identifier cannot be empty.", nameof(productId));
        }

        ProductId = productId;
        Name = Required(name, nameof(name));
        Code = Required(code, nameof(code));
        RepositoryUrl = repositoryUrl?.Trim();
        AzureDevOpsProject = azureDevOpsProject?.Trim();
        AzureDevOpsRepositoryId = azureDevOpsRepositoryId;
        IsActive = true;
    }

    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? RepositoryUrl { get; private set; }

    public string? AzureDevOpsProject { get; private set; }

    public Guid? AzureDevOpsRepositoryId { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
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

public sealed class DeploymentEnvironment : Entity
{
    private DeploymentEnvironment()
    {
    }

    public DeploymentEnvironment(
        Guid id,
        string name,
        string code,
        string? description,
        bool isProduction)
        : base(id)
    {
        Name = Required(name, nameof(name));
        Code = Required(code, nameof(code));
        Description = description?.Trim();
        IsProduction = isProduction;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsProduction { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
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
