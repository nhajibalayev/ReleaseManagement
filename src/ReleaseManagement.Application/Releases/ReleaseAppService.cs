using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.Releases;

public sealed class ReleaseAppService : IReleaseAppService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IReleaseNumberGenerator _releaseNumberGenerator;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly IValidator<CreateReleaseDraftRequest> _createValidator;
    private readonly IValidator<UpdateReleaseDraftRequest> _updateValidator;

    public ReleaseAppService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IReleaseAuthorizationService authorization,
        IReleaseNumberGenerator releaseNumberGenerator,
        IClock clock,
        IAuditService audit,
        IValidator<CreateReleaseDraftRequest> createValidator,
        IValidator<UpdateReleaseDraftRequest> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _authorization = authorization;
        _releaseNumberGenerator = releaseNumberGenerator;
        _clock = clock;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Guid> CreateDraftAsync(
        CreateReleaseDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authorization.EnsureCanCreateForProductAsync(request.ProductId, cancellationToken);
        await EnsureCatalogAsync(request, cancellationToken);

        var releaseNumber = await _releaseNumberGenerator.GenerateAsync(cancellationToken);
        var now = _clock.UtcNow;

        var release = new Release(
            Guid.NewGuid(),
            releaseNumber,
            request.Title,
            request.Description,
            request.ProductId,
            request.EnvironmentId,
            request.PlannedReleaseDateUtc,
            _currentUser.UserId,
            now);

        ApplyDraftFields(release, request, now);
        ApplyServices(release, request.Services, now);

        _dbContext.Releases.Add(release);

        await _audit.WriteAsync(
            "Release.CreateDraft",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { release.ReleaseNumber, release.Title, release.ProductId },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return release.Id;
    }

    public async Task UpdateDraftAsync(
        UpdateReleaseDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authorization.EnsureCanEditAsync(request.ReleaseId, cancellationToken);

        var release = await _dbContext.Releases
            .Include(item => item.Services)
            .SingleOrDefaultAsync(item => item.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), request.ReleaseId);
        }

        if (release.ProductId != request.ProductId)
        {
            throw new BusinessRuleException("Product cannot be changed after the draft is created.");
        }

        await EnsureCatalogAsync(request, cancellationToken);

        var now = _clock.UtcNow;
        ApplyDraftFields(release, request, now);
        release.ClearServices(now);
        ApplyServices(release, request.Services, now);

        await _audit.WriteAsync(
            "Release.UpdateDraft",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { release.Title, release.CurrentStatus },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReleaseDetailsDto> GetByIdAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        var release = await _dbContext.Releases
            .AsNoTracking()
            .Include(item => item.Services)
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), releaseId);
        }

        var status = await BuildStatusSummaryAsync(release, cancellationToken);

        return new ReleaseDetailsDto
        {
            Id = release.Id,
            ReleaseNumber = release.ReleaseNumber,
            Title = release.Title,
            Description = release.Description,
            ProductId = release.ProductId,
            EnvironmentId = release.EnvironmentId,
            ReleaseType = release.ReleaseType,
            Priority = release.Priority,
            ReleaseVersion = release.ReleaseVersion,
            BusinessReason = release.BusinessReason,
            ImpactDescription = release.ImpactDescription,
            TestingSummary = release.TestingSummary,
            RiskLevel = release.RiskLevel,
            RiskDescription = release.RiskDescription,
            DeploymentPlan = release.DeploymentPlan,
            RollbackPlan = release.RollbackPlan,
            MonitoringPlan = release.MonitoringPlan,
            PostReleaseValidationPlan = release.PostReleaseValidationPlan,
            DowntimeRequired = release.DowntimeRequired,
            ExpectedDowntimeMinutes = release.ExpectedDowntimeMinutes,
            PlannedReleaseDate = release.PlannedReleaseDate,
            ActualReleaseDate = release.ActualReleaseDate,
            CreatedByUserId = release.CreatedByUserId,
            AzureDevOpsWorkItemId = release.AzureDevOpsWorkItemId,
            AzureDevOpsWorkItemUrl = release.AzureDevOpsWorkItemUrl,
            Status = status,
            Services = release.Services
                .Select(service => new ReleaseServiceInputDto
                {
                    ServiceId = service.ServiceId,
                    BranchName = service.BranchName,
                    CommitId = service.CommitId,
                    Version = service.Version,
                    BuildNumber = service.BuildNumber,
                    ArtifactUrl = service.ArtifactUrl,
                    RepositoryUrl = service.RepositoryUrl,
                    DatabaseChanges = service.DatabaseChanges,
                    ConfigurationChanges = service.ConfigurationChanges,
                    Notes = service.Notes
                })
                .ToArray(),
            AllowedTransitions = ReleaseWorkflowRules.GetAllowedTargets(
                release.CurrentStatus,
                _currentUser.Roles)
        };
    }

    public async Task<ReleaseStatusSummaryDto> GetStatusSummaryAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        var release = await _dbContext.Releases
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), releaseId);
        }

        return await BuildStatusSummaryAsync(release, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReleaseListItemDto>> GetMyReleasesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var query = _dbContext.Releases.AsNoTracking();

        if (!_currentUser.IsInRole(RoleNames.Administrator) &&
            !_currentUser.IsInRole(RoleNames.ReleaseManager) &&
            !_currentUser.IsInRole(RoleNames.Auditor))
        {
            var accessibleProductIds = await _dbContext.UserProductAccesses
                .AsNoTracking()
                .Where(access => access.UserId == _currentUser.UserId)
                .Select(access => access.ProductId)
                .ToListAsync(cancellationToken);

            query = query.Where(release =>
                release.CreatedByUserId == _currentUser.UserId ||
                accessibleProductIds.Contains(release.ProductId));
        }

        var releases = await query
            .OrderByDescending(release => release.UpdatedDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        var productNames = await _dbContext.Products
            .AsNoTracking()
            .Where(product => releases.Select(release => release.ProductId).Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, product => product.Name, cancellationToken);

        return releases
            .Select(release =>
            {
                var actionRequired = IsActionRequired(release);
                return new ReleaseListItemDto
                {
                    Id = release.Id,
                    ReleaseNumber = release.ReleaseNumber,
                    Title = release.Title,
                    ProductId = release.ProductId,
                    ProductName = productNames.GetValueOrDefault(release.ProductId),
                    ReleaseVersion = release.ReleaseVersion,
                    PlannedReleaseDate = release.PlannedReleaseDate,
                    CurrentStatus = release.CurrentStatus,
                    CurrentStatusDisplay = ReleaseStatusDisplay.Format(release.CurrentStatus),
                    CurrentResponsibleUserId = release.CurrentResponsibleUserId,
                    CurrentResponsibleRole = release.CurrentResponsibleRole,
                    CurrentResponsibleDisplay = ReleaseStatusDisplay.FormatResponsible(
                        release.CurrentResponsibleRole,
                        release.CurrentResponsibleUserId),
                    UpdatedDate = release.UpdatedDate,
                    ActionRequiredFromCurrentUser = actionRequired
                };
            })
            .ToArray();
    }

    private async Task<ReleaseStatusSummaryDto> BuildStatusSummaryAsync(
        Release release,
        CancellationToken cancellationToken)
    {
        var latestHistory = await _dbContext.ReleaseStatusHistories
            .AsNoTracking()
            .Where(item => item.ReleaseId == release.Id)
            .OrderByDescending(item => item.ChangedDate)
            .FirstOrDefaultAsync(cancellationToken);

        string? returnReason = null;
        string? returnedBy = null;
        DateTime? returnedDate = null;

        if (release.CurrentStatus is
            ReleaseStatus.ReturnedForRevision or
            ReleaseStatus.Rejected or
            ReleaseStatus.PentestChangesRequired or
            ReleaseStatus.InfoSecChangesRequired or
            ReleaseStatus.BusinessChangesRequired)
        {
            returnReason = latestHistory?.Comment;
            returnedDate = latestHistory?.ChangedDate;
            returnedBy = latestHistory is null
                ? null
                : ReleaseStatusDisplay.FormatResponsible(
                    null,
                    latestHistory.ChangedByUserId);
        }

        return new ReleaseStatusSummaryDto
        {
            ReleaseId = release.Id,
            ReleaseNumber = release.ReleaseNumber,
            CurrentStatus = release.CurrentStatus,
            CurrentStatusDisplay = ReleaseStatusDisplay.Format(release.CurrentStatus),
            CurrentResponsibleDisplay = ReleaseStatusDisplay.FormatResponsible(
                release.CurrentResponsibleRole,
                release.CurrentResponsibleUserId),
            StatusChangedAtUtc = latestHistory?.ChangedDate ?? release.UpdatedDate,
            NextStageDisplay = ReleaseStatusDisplay.GetNextStageDisplay(release.CurrentStatus),
            ActionRequiredFromCurrentUser = IsActionRequired(release),
            ReturnOrRejectReason = returnReason,
            ReturnedByDisplay = returnedBy,
            ReturnedDateUtc = returnedDate,
            PlannedReleaseDateUtc = release.PlannedReleaseDate
        };
    }

    private bool IsActionRequired(Release release)
    {
        if (release.CurrentResponsibleUserId == _currentUser.UserId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(release.CurrentResponsibleRole) &&
            _currentUser.IsInRole(release.CurrentResponsibleRole))
        {
            return true;
        }

        return ReleaseWorkflowRules.GetAllowedTargets(release.CurrentStatus, _currentUser.Roles).Count > 0
               && release.CurrentStatus is not (
                   ReleaseStatus.Draft or
                   ReleaseStatus.Closed or
                   ReleaseStatus.Cancelled or
                   ReleaseStatus.Rejected or
                   ReleaseStatus.RolledBack);
    }

    private async Task EnsureCatalogAsync(
        CreateReleaseDraftRequest request,
        CancellationToken cancellationToken)
    {
        var productExists = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == request.ProductId && product.IsActive, cancellationToken);

        if (!productExists)
        {
            throw new BusinessRuleException("Product is missing or inactive.");
        }

        var environmentExists = await _dbContext.Environments
            .AsNoTracking()
            .AnyAsync(
                environment => environment.Id == request.EnvironmentId && environment.IsActive,
                cancellationToken);

        if (!environmentExists)
        {
            throw new BusinessRuleException("Environment is missing or inactive.");
        }

        if (request.Services.Count == 0)
        {
            return;
        }

        var serviceIds = request.Services.Select(service => service.ServiceId).Distinct().ToArray();
        var matchingCount = await _dbContext.Services
            .AsNoTracking()
            .CountAsync(
                service => serviceIds.Contains(service.Id) &&
                           service.ProductId == request.ProductId &&
                           service.IsActive,
                cancellationToken);

        if (matchingCount != serviceIds.Length)
        {
            throw new BusinessRuleException(
                "One or more services are invalid for the selected product.");
        }
    }

    private static void ApplyDraftFields(
        Release release,
        CreateReleaseDraftRequest request,
        DateTime now)
    {
        release.UpdateGeneralInformation(
            request.Title,
            request.Description,
            request.ReleaseType,
            request.Priority,
            request.EnvironmentId,
            request.PlannedReleaseDateUtc,
            request.ReleaseVersion ?? string.Empty,
            request.BusinessReason ?? string.Empty,
            request.ImpactDescription ?? string.Empty,
            now);

        release.UpdateReadiness(
            request.TestingSummary ?? string.Empty,
            request.RiskLevel,
            request.RiskDescription ?? string.Empty,
            request.DeploymentPlan ?? string.Empty,
            request.RollbackPlan ?? string.Empty,
            request.MonitoringPlan ?? string.Empty,
            request.PostReleaseValidationPlan ?? string.Empty,
            request.DowntimeRequired,
            request.ExpectedDowntimeMinutes,
            now);
    }

    private static void ApplyServices(
        Release release,
        IEnumerable<ReleaseServiceInputDto> services,
        DateTime now)
    {
        foreach (var serviceInput in services)
        {
            var releaseService = new Domain.Entities.ReleaseService(
                Guid.NewGuid(),
                release.Id,
                serviceInput.ServiceId);

            releaseService.SetBuildInformation(
                serviceInput.BranchName,
                serviceInput.CommitId,
                serviceInput.Version,
                serviceInput.BuildNumber,
                serviceInput.ArtifactUrl,
                serviceInput.RepositoryUrl);

            releaseService.SetChangeFlags(
                serviceInput.DatabaseChanges,
                serviceInput.ConfigurationChanges,
                serviceInput.Notes);

            release.AddService(releaseService, now);
        }
    }
}
