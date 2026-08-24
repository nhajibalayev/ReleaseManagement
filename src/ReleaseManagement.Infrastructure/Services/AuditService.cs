using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IClock clock,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task WriteAsync(
        string action,
        string entityName,
        string entityId,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId =
            httpContext?.TraceIdentifier ??
            Guid.NewGuid().ToString("N");

        var entry = new AuditLog(
            Guid.NewGuid(),
            _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            action,
            entityName,
            entityId,
            oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            newValues is null ? null : JsonSerializer.Serialize(newValues),
            httpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContext?.Request.Headers.UserAgent.ToString(),
            _clock.UtcNow,
            correlationId);

        _dbContext.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}
