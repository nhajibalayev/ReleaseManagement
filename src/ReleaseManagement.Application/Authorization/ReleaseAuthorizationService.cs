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
    private readonly IProductAccessService _productAccess;

    public ReleaseAuthorizationService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IAzureDevOpsProjectAccessService azureDevOpsProjectAccess,
        IProductAccessService productAccess)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _azureDevOpsProjectAccess = azureDevOpsProjectAccess;
        _productAccess = productAccess;
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

        // Product membership with at least "Edit" (CreateRelease) is required; Administrator is exempt.
        if (!_currentUser.IsInRole(RoleNames.Administrator))
        {
            var access = await _productAccess.GetAccessTypeAsync(productId, cancellationToken);
            if (access is null || access < ProductAccessType.CreateRelease)
            {
                throw new ForbiddenException(
                    "You do not have 'Edit' access to this product. Ask an administrator to add you as a product member.");
            }
        }

        // Azure DevOps project access is still checked when integration is enforced.
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

        var isCreator = release.CreatedByUserId == _currentUser.UserId;
        var isResponsibleUser = release.CurrentResponsibleUserId == _currentUser.UserId;
        var productAccess = await _productAccess.GetAccessTypeAsync(release.ProductId, cancellationToken);

        // Product scoping: unless the user is personally involved (creator / explicitly assigned),
        // a release of a product they are not a member of does not exist for them.
        if (!isCreator && !isResponsibleUser && productAccess is null)
        {
            return ReleaseAccessLevel.None;
        }

        var isEditableStatus = release.CurrentStatus is ReleaseStatus.Draft or ReleaseStatus.ReturnedForRevision;

        if (isCreator && isEditableStatus)
        {
            return ReleaseAccessLevel.Edit;
        }

        if (isResponsibleUser)
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

        // "Manage" members may edit any draft of their product; "Edit" members only their own (handled above).
        if (productAccess == ProductAccessType.Manage && isEditableStatus)
        {
            return ReleaseAccessLevel.Edit;
        }

        return ReleaseAccessLevel.View;
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

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            throw new ForbiddenException("Authentication is required.");
        }
    }
}
