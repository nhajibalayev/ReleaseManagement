using FluentValidation.TestHelper;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Application.Validation;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Tests;

public sealed class CreateReleaseDraftRequestValidatorTests
{
    private readonly CreateReleaseDraftRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var request = CreateValidRequest();

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Requires_title_product_and_utc_date()
    {
        var request = CreateValidRequest();
        request.Title = " ";
        request.ProductId = Guid.Empty;
        request.PlannedReleaseDateUtc = DateTime.Now;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(item => item.Title);
        result.ShouldHaveValidationErrorFor(item => item.ProductId);
        result.ShouldHaveValidationErrorFor(item => item.PlannedReleaseDateUtc);
    }

    [Fact]
    public void Requires_risk_description_for_high_risk()
    {
        var request = CreateValidRequest();
        request.RiskLevel = RiskLevel.High;
        request.RiskDescription = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(item => item.RiskDescription);
    }

    [Fact]
    public void Requires_expected_downtime_when_downtime_is_planned()
    {
        var request = CreateValidRequest();
        request.DowntimeRequired = true;
        request.ExpectedDowntimeMinutes = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(item => item.ExpectedDowntimeMinutes);
    }

    private static CreateReleaseDraftRequest CreateValidRequest()
    {
        return new CreateReleaseDraftRequest
        {
            Title = "Payment release",
            Description = "Deploy payment service",
            ProductId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            PlannedReleaseDateUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            RiskLevel = RiskLevel.Medium,
            Services =
            [
                new ReleaseServiceInputDto
                {
                    ServiceId = Guid.NewGuid(),
                    BranchName = "release/1.0",
                    Version = "1.0.0"
                }
            ]
        };
    }
}
