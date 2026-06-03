namespace ReleaseHub.Domain.Enums;

public enum ReleaseTaskState
{
    Draft = 0,
    PendingInfoSec = 1,
    PendingITRisk = 2,
    PendingReleaseManager = 3,
    Approved = 4,
    Rejected = 5,
    Released = 6,
    Cancelled = 7
}
