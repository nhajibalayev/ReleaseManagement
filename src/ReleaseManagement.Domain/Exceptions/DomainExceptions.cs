using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class InvalidReleaseTransitionException : DomainException
{
    public InvalidReleaseTransitionException(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        string reason)
        : base(
            $"Release transition from {currentStatus} to {targetStatus} is not allowed. {reason}")
    {
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }

    public ReleaseStatus CurrentStatus { get; }

    public ReleaseStatus TargetStatus { get; }
}
