using ReleaseHub.Domain.Enums;

namespace ReleaseHub.Domain.Entities;

public class ApprovalStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReleaseTaskId { get; set; }
    public int StepOrder { get; set; }
    public ApprovalRole Role { get; set; }
    public string? ApproverUserId { get; set; }
    public string? ApproverEmail { get; set; }
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public string? Comment { get; set; }
    public DateTime? DecidedAt { get; set; }
}
