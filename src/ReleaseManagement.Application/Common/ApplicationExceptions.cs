namespace ReleaseManagement.Application.Common;

public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : ApplicationException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}

public sealed class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class ConflictException : ApplicationException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

public sealed class BusinessRuleException : ApplicationException
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }

    public BusinessRuleException(IEnumerable<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyCollection<string> Errors { get; } = [];
}
