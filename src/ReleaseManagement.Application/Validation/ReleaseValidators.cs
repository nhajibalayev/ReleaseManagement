using FluentValidation;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Validation;

public sealed class CreateReleaseDraftRequestValidator : AbstractValidator<CreateReleaseDraftRequest>
{
    public CreateReleaseDraftRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(request => request.ProductId)
            .NotEmpty();

        RuleFor(request => request.EnvironmentId)
            .NotEmpty();

        RuleFor(request => request.PlannedWindowStartUtc)
            .Must(date => date.Kind == DateTimeKind.Utc)
            .WithMessage("Planned window start must be UTC.");

        RuleFor(request => request.PlannedWindowEndUtc)
            .Must(date => date.Kind == DateTimeKind.Utc)
            .WithMessage("Planned window end must be UTC.")
            .GreaterThan(request => request.PlannedWindowStartUtc)
            .WithMessage("Planned window end must be after the window start.");

        RuleFor(request => request.ExpeditedJustification)
            .NotEmpty()
            .When(request => request.ExecutionMode == ExecutionMode.Expedited)
            .WithMessage("Expedited execution requires an urgency justification (§6.2).");

        RuleFor(request => request.ExpeditedJustification)
            .MaximumLength(2000);

        RuleFor(request => request.DirectorApprovalReference)
            .MaximumLength(500);

        RuleFor(request => request.MaintenanceApprovalReference)
            .MaximumLength(500);

        RuleFor(request => request.KeyDependencies)
            .MaximumLength(2000);

        RuleFor(request => request.RecoveryDecisionPoints)
            .MaximumLength(4000);

        RuleFor(request => request.RecoveryResponsibleParties)
            .MaximumLength(1000);

        RuleForEach(request => request.References)
            .SetValidator(new ReleaseReferenceInputDtoValidator());

        RuleFor(request => request.ReleaseVersion)
            .MaximumLength(100);

        RuleFor(request => request.BusinessReason)
            .MaximumLength(2000);

        RuleFor(request => request.ImpactDescription)
            .MaximumLength(2000);

        RuleFor(request => request.TestingSummary)
            .MaximumLength(4000);

        RuleFor(request => request.RiskDescription)
            .MaximumLength(2000);

        RuleFor(request => request.DeploymentPlan)
            .MaximumLength(4000);

        RuleFor(request => request.RollbackPlan)
            .MaximumLength(4000);

        RuleFor(request => request.MonitoringPlan)
            .MaximumLength(2000);

        RuleFor(request => request.PostReleaseValidationPlan)
            .MaximumLength(2000);

        RuleFor(request => request.ExpectedDowntimeMinutes)
            .NotNull()
            .When(request => request.DowntimeRequired)
            .WithMessage("Expected downtime is required when downtime is planned.");

        RuleFor(request => request.ExpectedDowntimeMinutes)
            .GreaterThanOrEqualTo(0)
            .When(request => request.ExpectedDowntimeMinutes.HasValue);

        RuleFor(request => request.RiskDescription)
            .NotEmpty()
            .When(request => request.RiskLevel is RiskLevel.High or RiskLevel.Critical)
            .WithMessage("Risk description is required for high or critical risk.");

        RuleForEach(request => request.Services)
            .SetValidator(new ReleaseServiceInputDtoValidator());
    }
}

public sealed class UpdateReleaseDraftRequestValidator : AbstractValidator<UpdateReleaseDraftRequest>
{
    public UpdateReleaseDraftRequestValidator()
    {
        Include(new CreateReleaseDraftRequestValidator());

        RuleFor(request => request.ReleaseId)
            .NotEmpty();
    }
}

public sealed class ReleaseServiceInputDtoValidator : AbstractValidator<ReleaseServiceInputDto>
{
    public ReleaseServiceInputDtoValidator()
    {
        RuleFor(service => service.ServiceId)
            .NotEmpty();

        RuleFor(service => service.BranchName)
            .MaximumLength(200);

        RuleFor(service => service.CommitId)
            .MaximumLength(100);

        RuleFor(service => service.Version)
            .MaximumLength(100);

        RuleFor(service => service.BuildNumber)
            .MaximumLength(100);

        RuleFor(service => service.ArtifactUrl)
            .MaximumLength(500);

        RuleFor(service => service.RepositoryUrl)
            .MaximumLength(2048);

        RuleFor(service => service.Notes)
            .MaximumLength(2000);
    }
}

public sealed class ReleaseReferenceInputDtoValidator : AbstractValidator<ReleaseReferenceInputDto>
{
    public ReleaseReferenceInputDtoValidator()
    {
        RuleFor(reference => reference.ExternalId)
            .MaximumLength(200);

        RuleFor(reference => reference.Url)
            .MaximumLength(2048);

        RuleFor(reference => reference.Title)
            .MaximumLength(500);
    }
}

public sealed class TransitionReleaseRequestValidator : AbstractValidator<TransitionReleaseRequest>
{
    public TransitionReleaseRequestValidator()
    {
        RuleFor(request => request.ReleaseId)
            .NotEmpty();

        RuleFor(request => request.TargetStatus)
            .IsInEnum();

        RuleFor(request => request.Comment)
            .MaximumLength(4000);
    }
}
