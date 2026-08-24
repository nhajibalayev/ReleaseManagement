namespace ReleaseManagement.Infrastructure.Persistence;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
