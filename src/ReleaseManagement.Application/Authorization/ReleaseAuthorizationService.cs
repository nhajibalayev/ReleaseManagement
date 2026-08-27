using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.Authorization;

public sealed class ReleaseAuthorizationService : IReleaseAuthorizationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAzureDevOpsProjectAccessService _azureDevOpsProjectAccess;

    public ReleaseAuthorizationService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IAzureDevOpsProjectAccessService azureDevOpsProjectAccess)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _azureDevOpsProjectAccess = azureDevOpsProjectAccess;
    }

    public async Task EnsureCanViewAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var level = await GetAccessLevelAsync(releaseId, cancellationToken);
        if (level == ReleaseAccessLevel.None)
        {
            throw new ForbiddenException(
                $"You are not allowed to view release '{releaseId}'.");
        }
    }

    public async Task EnsureCanEditAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var level = await GetAccessLevelAsync(releaseId, cancellationToken);
        if (level < ReleaseAccessLevel.Edit)
        {
            throw new ForbiddenException(
                $"You are not allowed to edit release '{releaseId}'.");
        }
    }

    public async Task EnsureCanCreateForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        // Open create: any signed-in user may create a release for any active product.
        // Azure DevOps project access is still checked when integration is enforced.
        _ = productId;
        await _azureDevOpsProjectAccess.EnsureCurrentUserCanAccessProjectAsync(cancellationToken);
    }

    public async Task<bool> CanViewAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var level = await GetAccessLevelAsync(releaseId, cancellationToken);
        return level != ReleaseAccessLevel.None;
    }

    public async Task<ReleaseAccessLevel> GetAccessLevelAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var release = await _dbContext.Releases
            .AsNoTracking()
            .Where(item => item.Id == releaseId)
            .Select(item => new
            {
                item.Id,
                item.CreatedByUserId,
                item.ProductId,
                item.CurrentStatus,
                item.CurrentResponsibleUserId,
                item.CurrentResponsibleRole
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), releaseId);
        }

        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            return ReleaseAccessLevel.Transition;
        }

        if (_currentUser.IsInRole(RoleNames.ReleaseManager) ||
            _currentUser.IsInRole(RoleNames.Auditor))
        {
            return ReleaseAccessLevel.View;
        }

        if (release.CreatedByUserId == _currentUser.UserId)
        {
            return release.CurrentStatus is ReleaseStatus.Draft or ReleaseStatus.ReturnedForRevision
                ? ReleaseAccessLevel.Edit
                : ReleaseAccessLevel.View;
        }

        if (release.CurrentResponsibleUserId == _currentUser.UserId)
        {
            return ReleaseAccessLevel.Transition;
        }

        if (!string.IsNullOrWhiteSpace(release.CurrentResponsibleRole) &&
            _currentUser.IsInRole(release.CurrentResponsibleRole))
        {
            return ReleaseAccessLevel.Transition;
        }

        if (CanActOnStatus(release.CurrentStatus))
        {
            return ReleaseAccessLevel.Transition;
        }

        var productAccess = await _dbContext.UserProductAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserId == _currentUser.UserId &&
                access.ProductId == release.ProductId)
            .Select(access => (ProductAccessType?)access.AccessType)
            .FirstOrDefaultAsync(cancellationToken);

        if (productAccess is null)
        {
            return ReleaseAccessLevel.None;
        }

        return productAccess >= ProductAccessType.CreateRelease
            ? ReleaseAccessLevel.View
            : ReleaseAccessLevel.View;
    }

    public bool CanActOnStatus(ReleaseStatus status)
    {
        EnsureAuthenticated();

        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            return true;
        }

        var allowed = ReleaseWorkflowRules.GetAllowedTargets(status, _currentUser.Roles);
        return allowed.Count > 0;
    }

    private bool IsPrivilegedViewerOrAdmin()
    {
        return _currentUser.IsInRole(RoleNames.Administrator) ||
               _currentUser.IsInRole(RoleNames.ReleaseManager);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new ForbiddenException("Authentication is required.");
        }
    }
}
