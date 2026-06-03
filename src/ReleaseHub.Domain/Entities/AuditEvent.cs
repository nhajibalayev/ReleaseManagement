namespace ReleaseHub.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReleaseTaskId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
