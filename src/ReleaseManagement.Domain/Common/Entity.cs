namespace ReleaseManagement.Domain.Common;

public abstract class Entity
{
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; protected set; }
}

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id, DateTime createdDateUtc)
        : base(id)
    {
        CreatedDate = EnsureUtc(createdDateUtc, nameof(createdDateUtc));
        UpdatedDate = CreatedDate;
    }

    public DateTime CreatedDate { get; protected set; }

    public DateTime UpdatedDate { get; protected set; }

    protected void MarkUpdated(DateTime updatedDateUtc)
    {
        UpdatedDate = EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }

    protected static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
    }
}
