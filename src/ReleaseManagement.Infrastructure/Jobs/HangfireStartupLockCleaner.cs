using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ReleaseManagement.Infrastructure.Jobs;

/// <summary>
/// Clears orphaned Hangfire distributed locks left behind when the process is
/// killed (Visual Studio Stop Debug, crash, etc.). Safe for single-instance deployments.
/// </summary>
public sealed class HangfireStartupLockCleaner
{
    private readonly string _connectionString;
    private readonly ILogger<HangfireStartupLockCleaner> _logger;

    public HangfireStartupLockCleaner(
        IConfiguration configuration,
        ILogger<HangfireStartupLockCleaner> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _logger = logger;
    }

    public async Task ClearStaleLocksAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Table is created as hangfire.lock by Hangfire.PostgreSql.
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM hangfire.lock;
            """,
            connection);

        try
        {
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Cleared {Count} stale Hangfire lock(s).", deleted);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            _logger.LogDebug(exception, "Hangfire lock table does not exist yet; nothing to clear.");
        }
    }
}
