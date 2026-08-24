using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Approvals;
using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Approvals;

public interface IApprovalService
{
    Task DecideAsync(
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PendingApprovalDto>> GetPendingAsync(
        CancellationToken cancellationToken = default);
}

public sealed class ApprovalService : IApprovalService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IReleaseWorkflowService _workflow;
    private readonly IValidator<ApprovalDecisionRequest> _validator;

    public ApprovalService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IReleaseAuthorizationService authorization,
        IReleaseWorkflowService workflow,
        IValidator<ApprovalDecisionRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _authorization = authorization;
        _workflow = workflow;
        _validator = validator;
    }

    public async Task DecideAsync(
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        var release = await _dbContext.Releases
            .SingleOrDefaultAsync(item => item.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), request.ReleaseId);
        }

        var expectedStatus = request.ApprovalType switch
        {
            ApprovalType.ReleaseManager => ReleaseStatus.ReleaseManagerReview,
            ApprovalType.Pentest => ReleaseStatus.PentestReview,
            ApprovalType.InfoSec => ReleaseStatus.InfoSecReview,
            ApprovalType.Business => ReleaseStatus.BusinessApproval,
            _ => throw new BusinessRuleException("Unsupported approval type.")
        };

        if (release.CurrentStatus != expectedStatus)
        {
            throw new ConflictException(
                $"Release '{release.ReleaseNumber}' is not awaiting {request.ApprovalType} approval.");
        }

        var targetStatus = ResolveTargetStatus(request.ApprovalType, request.Decision);

        await _workflow.TransitionAsync(
            new DTOs.Releases.TransitionReleaseRequest
            {
                ReleaseId = request.ReleaseId,
                TargetStatus = targetStatus,
                Comment = request.Comment
            },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<PendingApprovalDto>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var statuses = GetStatusesForCurrentUser();
        if (statuses.Count == 0 && !_currentUser.IsInRole(RoleNames.Administrator))
        {
            return [];
        }

        var query = _dbContext.Releases.AsNoTracking()
            .Where(release => statuses.Contains(release.CurrentStatus));

        if (!_currentUser.IsInRole(RoleNames.Administrator) &&
            !_currentUser.IsInRole(RoleNames.ReleaseManager))
        {
            var userId = _currentUser.UserId;
            var roles = _currentUser.Roles.ToArray();

            query = query.Where(release =>
                release.CurrentResponsibleUserId == userId ||
                (release.CurrentResponsibleRole != null &&
                 roles.Contains(release.CurrentResponsibleRole)));
        }

        var releases = await query
            .OrderBy(release => release.PlannedReleaseDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        var productNames = await _dbContext.Products
            .AsNoTracking()
            .Where(product => releases.Select(release => release.ProductId).Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, product => product.Name, cancellationToken);

        return releases
            .Select(release => new PendingApprovalDto
            {
                ApprovalId = release.Id,
                ReleaseId = release.Id,
                ReleaseNumber = release.ReleaseNumber,
                Title = release.Title,
                ProductId = release.ProductId,
                ProductName = productNames.GetValueOrDefault(release.ProductId),
                RequesterUserId = release.CreatedByUserId,
                RequestedDate = release.SubmittedDate ?? release.CreatedDate,
                PlannedReleaseDate = release.PlannedReleaseDate,
                ApprovalType = MapStatusToApprovalType(release.CurrentStatus),
                SlaDueDate = (release.SubmittedDate ?? release.CreatedDate).AddBusinessDays(2)
            })
            .ToArray();
    }

    private IReadOnlyCollection<ReleaseStatus> GetStatusesForCurrentUser()
    {
        var statuses = new List<ReleaseStatus>();

        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.ReleaseManager))
        {
            statuses.Add(ReleaseStatus.ReleaseManagerReview);
            statuses.Add(ReleaseStatus.Submitted);
        }

        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.Pentest))
        {
            statuses.Add(ReleaseStatus.PentestReview);
        }

        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.InfoSec))
        {
            statuses.Add(ReleaseStatus.InfoSecReview);
        }

        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.BusinessApprover))
        {
            statuses.Add(ReleaseStatus.BusinessApproval);
        }

        return statuses.Distinct().ToArray();
    }

    private static ReleaseStatus ResolveTargetStatus(
        ApprovalType approvalType,
        ApprovalStatus decision)
    {
        return (approvalType, decision) switch
        {
            (ApprovalType.ReleaseManager, ApprovalStatus.Approved) => ReleaseStatus.PentestReview,
            (ApprovalType.ReleaseManager, ApprovalStatus.ChangesRequired) =>
                ReleaseStatus.ReturnedForRevision,
            (ApprovalType.ReleaseManager, ApprovalStatus.Rejected) => ReleaseStatus.Rejected,

            (ApprovalType.Pentest, ApprovalStatus.Approved) => ReleaseStatus.InfoSecReview,
            (ApprovalType.Pentest, ApprovalStatus.ChangesRequired) =>
                ReleaseStatus.PentestChangesRequired,
            (ApprovalType.Pentest, ApprovalStatus.Rejected) => ReleaseStatus.Rejected,

            (ApprovalType.InfoSec, ApprovalStatus.Approved) => ReleaseStatus.BusinessApproval,
            (ApprovalType.InfoSec, ApprovalStatus.ChangesRequired) =>
                ReleaseStatus.InfoSecChangesRequired,
            (ApprovalType.InfoSec, ApprovalStatus.Rejected) => ReleaseStatus.Rejected,

            (ApprovalType.Business, ApprovalStatus.Approved) => ReleaseStatus.Approved,
            (ApprovalType.Business, ApprovalStatus.ChangesRequired) =>
                ReleaseStatus.BusinessChangesRequired,
            (ApprovalType.Business, ApprovalStatus.Rejected) => ReleaseStatus.Rejected,

            _ => throw new BusinessRuleException("Unsupported approval decision.")
        };
    }

    private static ApprovalType MapStatusToApprovalType(ReleaseStatus status) => status switch
    {
        ReleaseStatus.Submitted or ReleaseStatus.ReleaseManagerReview => ApprovalType.ReleaseManager,
        ReleaseStatus.PentestReview => ApprovalType.Pentest,
        ReleaseStatus.InfoSecReview => ApprovalType.InfoSec,
        ReleaseStatus.BusinessApproval => ApprovalType.Business,
        _ => ApprovalType.ReleaseManager
    };
}

internal static class DateTimeExtensions
{
    public static DateTime AddBusinessDays(this DateTime date, int days)
    {
        var result = date;
        var remaining = days;

        while (remaining > 0)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                remaining--;
            }
        }

        return result;
    }
}
