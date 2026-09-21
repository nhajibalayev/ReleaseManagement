using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Planning;

/// <summary>Procedure v4.0 §3.1 (quarterly forecast) and §6.3 (freeze periods).</summary>
public interface IPlanningService
{
    Task<IReadOnlyCollection<ReleaseForecastDto>> GetForecastAsync(int year, int quarter, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReleaseForecastDto>> GetOpenForecastsForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Guid> SaveForecastAsync(SaveForecastRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FreezePeriodDto>> GetFreezePeriodsAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<Guid> CreateFreezePeriodAsync(CreateFreezePeriodRequest request, CancellationToken cancellationToken = default);

    Task DeactivateFreezePeriodAsync(Guid freezePeriodId, CancellationToken cancellationToken = default);

    Task AddFreezeExceptionAsync(AddFreezeExceptionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FreezeConflictDto>> GetFreezeConflictsAsync(
        Guid? releaseId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken cancellationToken = default);
}

public sealed class PlanningService : IPlanningService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IAuditService _audit;

    public PlanningService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IClock clock,
        IAuditService audit)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<IReadOnlyCollection<ReleaseForecastDto>> GetForecastAsync(
        int year,
        int quarter,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.ReleaseForecasts
            .AsNoTracking()
            .Where(item => item.Year == year && item.Quarter == quarter)
            .OrderBy(item => item.ExpectedDate)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);

        return await MapForecastsAsync(items, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReleaseForecastDto>> GetOpenForecastsForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.ReleaseForecasts
            .AsNoTracking()
            .Where(item =>
                item.ProductId == productId &&
                item.Status != ForecastStatus.Cancelled &&
                item.Status != ForecastStatus.Delivered)
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Quarter)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);

        return await MapForecastsAsync(items, cancellationToken);
    }

    public async Task<Guid> SaveForecastAsync(SaveForecastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Team))
        {
            throw new BusinessRuleException("Title and team are required.");
        }

        if (request.Quarter is < 1 or > 4)
        {
            throw new BusinessRuleException("Quarter must be between 1 and 4.");
        }

        var now = _clock.UtcNow;
        var expected = request.ExpectedDateUtc.HasValue
            ? DateTime.SpecifyKind(request.ExpectedDateUtc.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        if (request.Id is { } id && id != Guid.Empty)
        {
            var existing = await _dbContext.ReleaseForecasts
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(ReleaseForecast), id);

            EnsureCanEditForecast(existing);
            existing.Update(
                request.Title,
                request.Category,
                request.Team,
                expected,
                request.Dependencies,
                request.Notes,
                request.Status,
                now);

            await _audit.WriteAsync("Forecast.Update", nameof(ReleaseForecast), existing.Id.ToString(), null, request, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var forecast = new ReleaseForecast(
            Guid.NewGuid(),
            request.Year,
            request.Quarter,
            request.ProductId,
            request.Title,
            request.Category,
            request.Team,
            expected,
            request.Dependencies,
            request.Notes,
            _currentUser.UserId,
            now);

        _dbContext.ReleaseForecasts.Add(forecast);
        await _audit.WriteAsync("Forecast.Create", nameof(ReleaseForecast), forecast.Id.ToString(), null, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return forecast.Id;
    }

    public async Task<IReadOnlyCollection<FreezePeriodDto>> GetFreezePeriodsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FreezePeriods.AsNoTracking().Include(item => item.Exceptions).AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(item => item.IsActive);
        }

        var items = await query
            .OrderByDescending(item => item.StartDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new FreezePeriodDto
            {
                Id = item.Id,
                Name = item.Name,
                FreezeType = item.FreezeType,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Authority = item.Authority,
                Description = item.Description,
                IsActive = item.IsActive,
                ExceptionCount = item.Exceptions.Count
            })
            .ToArray();
    }

    public async Task<Guid> CreateFreezePeriodAsync(CreateFreezePeriodRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureFreezeManager();

        FreezePeriod freeze;
        try
        {
            freeze = new FreezePeriod(
                Guid.NewGuid(),
                request.Name,
                request.FreezeType,
                DateTime.SpecifyKind(request.StartDateUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(request.EndDateUtc, DateTimeKind.Utc),
                request.Authority,
                request.Description,
                _currentUser.UserId,
                _clock.UtcNow);
        }
        catch (ArgumentException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        _dbContext.FreezePeriods.Add(freeze);
        await _audit.WriteAsync("Freeze.Create", nameof(FreezePeriod), freeze.Id.ToString(), null, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return freeze.Id;
    }

    public async Task DeactivateFreezePeriodAsync(Guid freezePeriodId, CancellationToken cancellationToken = default)
    {
        EnsureFreezeManager();

        var freeze = await _dbContext.FreezePeriods
            .SingleOrDefaultAsync(item => item.Id == freezePeriodId, cancellationToken)
            ?? throw new NotFoundException(nameof(FreezePeriod), freezePeriodId);

        freeze.Deactivate();
        await _audit.WriteAsync("Freeze.Deactivate", nameof(FreezePeriod), freeze.Id.ToString(), null, null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddFreezeExceptionAsync(AddFreezeExceptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureFreezeManager();

        if (string.IsNullOrWhiteSpace(request.ApprovedBy) || string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new BusinessRuleException("The approving freeze authority and a justification are required (§6.3).");
        }

        var freeze = await _dbContext.FreezePeriods
            .Include(item => item.Exceptions)
            .SingleOrDefaultAsync(item => item.Id == request.FreezePeriodId, cancellationToken)
            ?? throw new NotFoundException(nameof(FreezePeriod), request.FreezePeriodId);

        var releaseExists = await _dbContext.Releases
            .AsNoTracking()
            .AnyAsync(item => item.Id == request.ReleaseId, cancellationToken);

        if (!releaseExists)
        {
            throw new NotFoundException(nameof(Release), request.ReleaseId);
        }

        var exception = new FreezeException(
            Guid.NewGuid(),
            freeze.Id,
            request.ReleaseId,
            request.ApprovedBy,
            request.Justification,
            _currentUser.UserId,
            _clock.UtcNow);

        try
        {
            freeze.AddException(exception);
        }
        catch (InvalidOperationException invalid)
        {
            throw new BusinessRuleException(invalid.Message);
        }

        _dbContext.FreezeExceptions.Add(exception);
        await _audit.WriteAsync("Freeze.Exception", nameof(FreezePeriod), freeze.Id.ToString(), null, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FreezeConflictDto>> GetFreezeConflictsAsync(
        Guid? releaseId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var freezes = await _dbContext.FreezePeriods
            .AsNoTracking()
            .Include(item => item.Exceptions)
            .Where(item => item.IsActive && item.StartDate < windowEndUtc && item.EndDate > windowStartUtc)
            .OrderBy(item => item.StartDate)
            .ToListAsync(cancellationToken);

        return freezes
            .Select(item => new FreezeConflictDto
            {
                FreezePeriodId = item.Id,
                Name = item.Name,
                FreezeType = item.FreezeType,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Authority = item.Authority,
                HasException = releaseId.HasValue && item.HasExceptionFor(releaseId.Value)
            })
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ReleaseForecastDto>> MapForecastsAsync(
        List<ReleaseForecast> items,
        CancellationToken cancellationToken)
    {
        var productIds = items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(item => productIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        var releaseIds = items.Where(item => item.ReleaseId.HasValue).Select(item => item.ReleaseId!.Value).Distinct().ToArray();
        var releases = releaseIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Releases
                .AsNoTracking()
                .Where(item => releaseIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.ReleaseNumber, cancellationToken);

        return items
            .Select(item => new ReleaseForecastDto
            {
                Id = item.Id,
                Year = item.Year,
                Quarter = item.Quarter,
                ProductId = item.ProductId,
                ProductName = products.GetValueOrDefault(item.ProductId),
                Title = item.Title,
                Category = item.Category,
                Team = item.Team,
                ExpectedDate = item.ExpectedDate,
                Dependencies = item.Dependencies,
                Notes = item.Notes,
                Status = item.Status,
                ReleaseId = item.ReleaseId,
                ReleaseNumber = item.ReleaseId.HasValue ? releases.GetValueOrDefault(item.ReleaseId.Value) : null
            })
            .ToArray();
    }

    private void EnsureCanEditForecast(ReleaseForecast forecast)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.ReleaseManager) ||
            forecast.CreatedByUserId == _currentUser.UserId)
        {
            return;
        }

        throw new ForbiddenException("Only the author or the Release Manager can edit this forecast entry.");
    }

    private void EnsureFreezeManager()
    {
        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.ReleaseManager))
        {
            return;
        }

        throw new ForbiddenException("Only the Release Manager can manage freeze periods on the calendar (§6.3).");
    }
}
