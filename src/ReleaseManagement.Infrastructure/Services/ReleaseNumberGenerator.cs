using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Services;

public sealed class ReleaseNumberGenerator : IReleaseNumberGenerator
{
    private static long _demoSequence;
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;

    public ReleaseNumberGenerator(ApplicationDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var demoValue = Interlocked.Increment(ref _demoSequence);
            return $"REL-{_clock.UtcNow.Year}-{demoValue:000000}";
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await _dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT nextval('release_number_seq')";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var sequenceValue = Convert.ToInt64(result);
            return $"REL-{_clock.UtcNow.Year}-{sequenceValue:000000}";
        }
        finally
        {
            if (shouldClose)
            {
                await _dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
