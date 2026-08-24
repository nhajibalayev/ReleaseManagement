namespace ReleaseManagement.Application.Abstractions;

public interface IReleaseNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
