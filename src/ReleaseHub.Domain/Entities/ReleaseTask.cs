using ReleaseHub.Domain.Enums;

namespace ReleaseHub.Domain.Entities;

public class ReleaseTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? AdoWorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReleaseTaskState State { get; set; } = ReleaseTaskState.Draft;
    public string AdoStateMirror { get; set; } = "New";
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CurrentAssigneeId { get; set; }
    public List<ProjectScope> ProjectScopes { get; set; } = new();
    public List<ApprovalStep> ApprovalSteps { get; set; } = new();
    public List<AuditEvent> AuditEvents { get; set; } = new();
}
