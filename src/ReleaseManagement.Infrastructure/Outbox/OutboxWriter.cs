using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;

    public OutboxWriter(ApplicationDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task EnqueueAsync(
        string messageType,
        string payloadJson,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var exists = await _dbContext.OutboxMessages
                .AsNoTracking()
                .AnyAsync(
                    message => message.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (exists)
            {
                return;
            }
        }

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            PayloadJson = payloadJson,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey,
            CreatedDate = _clock.UtcNow,
            Status = "Pending",
            AttemptCount = 0
        });
    }
}
