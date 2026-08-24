using FluentValidation;
using ReleaseManagement.Application.DTOs.Approvals;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Validation;

public sealed class ApprovalDecisionRequestValidator : AbstractValidator<ApprovalDecisionRequest>
{
    public ApprovalDecisionRequestValidator()
    {
        RuleFor(request => request.ReleaseId)
            .NotEmpty();

        RuleFor(request => request.ApprovalType)
            .IsInEnum();

        RuleFor(request => request.Decision)
            .Must(decision => decision is
                ApprovalStatus.Approved or
                ApprovalStatus.ChangesRequired or
                ApprovalStatus.Rejected)
            .WithMessage("Decision must be Approve, Changes Required, or Reject.");

        RuleFor(request => request.Comment)
            .NotEmpty()
            .When(request => request.Decision is
                ApprovalStatus.ChangesRequired or
                ApprovalStatus.Rejected)
            .WithMessage("A comment is required when approval is not granted.");

        RuleFor(request => request.Comment)
            .MaximumLength(4000);
    }
}
