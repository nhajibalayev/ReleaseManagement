using ReleaseManagement.Application.Abstractions;

namespace ReleaseManagement.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
