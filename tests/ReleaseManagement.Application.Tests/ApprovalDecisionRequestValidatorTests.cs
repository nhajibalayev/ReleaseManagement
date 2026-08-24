using FluentValidation.TestHelper;
using ReleaseManagement.Application.DTOs.Approvals;
using ReleaseManagement.Application.Validation;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Tests;

public sealed class ApprovalDecisionRequestValidatorTests
{
    private readonly ApprovalDecisionRequestValidator _validator = new();

    [Fact]
    public void Approve_without_comment_is_allowed()
    {
        var request = new ApprovalDecisionRequest
        {
            ReleaseId = Guid.NewGuid(),
            ApprovalType = ApprovalType.Pentest,
            Decision = ApprovalStatus.Approved
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Changes_required_needs_comment()
    {
        var request = new ApprovalDecisionRequest
        {
            ReleaseId = Guid.NewGuid(),
            ApprovalType = ApprovalType.InfoSec,
            Decision = ApprovalStatus.ChangesRequired,
            Comment = " "
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(item => item.Comment);
    }
}
