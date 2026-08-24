using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.DTOs.Approvals;

public sealed class ApprovalDecisionRequest
{
    public Guid ReleaseId { get; set; }

    public ApprovalType ApprovalType { get; set; }

    public ApprovalStatus Decision { get; set; }

    public string? Comment { get; set; }
}

public sealed class PendingApprovalDto
{
    public Guid ApprovalId { get; init; }

    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public Guid ProductId { get; init; }

    public string? ProductName { get; init; }

    public Guid RequesterUserId { get; init; }

    public DateTime RequestedDate { get; init; }

    public DateTime PlannedReleaseDate { get; init; }

    public ApprovalType ApprovalType { get; init; }

    public DateTime? SlaDueDate { get; init; }
}
