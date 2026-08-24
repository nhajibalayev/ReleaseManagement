namespace ReleaseManagement.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string entityName,
        string entityId,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken = default);
}
