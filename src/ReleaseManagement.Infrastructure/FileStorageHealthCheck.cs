using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure;

public sealed class FileStorageHealthCheck : IHealthCheck
{
    private readonly FileStorageOptions _options;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;

    public FileStorageHealthCheck(
        IOptions<FileStorageOptions> options,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(_environment.ContentRootPath, _options.RootPath);

        try
        {
            Directory.CreateDirectory(root);
            return Task.FromResult(HealthCheckResult.Healthy("File storage is available."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("File storage is unavailable.", exception));
        }
    }
}
