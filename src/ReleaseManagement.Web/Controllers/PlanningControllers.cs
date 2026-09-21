using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Application.Governance;
using ReleaseManagement.Application.Planning;
using ReleaseManagement.Application.PostRelease;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

/// <summary>Procedure v4.0 §3.1 — quarterly Release Forecast (a plan, not an approval).</summary>
[Authorize]
public sealed class ForecastsController : Controller
{
    private readonly IPlanningService _planning;
    private readonly IApplicationDbContext _dbContext;

    public ForecastsController(IPlanningService planning, IApplicationDbContext dbContext)
    {
        _planning = planning;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? quarter, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedQuarter = quarter ?? (now.Month - 1) / 3 + 1;

        return View(await BuildPageAsync(selectedYear, selectedQuarter, new ForecastFormViewModel
        {
            Year = selectedYear,
            Quarter = selectedQuarter
        }, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var forecast = await _dbContext.ReleaseForecasts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (forecast is null)
        {
            return NotFound();
        }

        return View("Index", await BuildPageAsync(forecast.Year, forecast.Quarter, new ForecastFormViewModel
        {
            Id = forecast.Id,
            Year = forecast.Year,
            Quarter = forecast.Quarter,
            ProductId = forecast.ProductId,
            Title = forecast.Title,
            Category = forecast.Category,
            Team = forecast.Team,
            ExpectedDate = forecast.ExpectedDate?.ToLocalTime().Date,
            Dependencies = forecast.Dependencies,
            Notes = forecast.Notes,
            Status = forecast.Status
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ForecastFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildPageAsync(form.Year, form.Quarter, form, cancellationToken));
        }

        try
        {
            await _planning.SaveForecastAsync(
                new SaveForecastRequest
                {
                    Id = form.Id,
                    Year = form.Year,
                    Quarter = form.Quarter,
                    ProductId = form.ProductId,
                    Title = form.Title,
                    Category = form.Category,
                    Team = form.Team,
                    ExpectedDateUtc = form.ExpectedDate.HasValue
                        ? DateTime.SpecifyKind(form.ExpectedDate.Value.Date, DateTimeKind.Utc)
                        : null,
                    Dependencies = form.Dependencies,
                    Notes = form.Notes,
                    Status = form.Status
                },
                cancellationToken);

            TempData["Success"] = "Forecast entry saved.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or NotFoundException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { year = form.Year, quarter = form.Quarter });
    }

    private async Task<ForecastPageViewModel> BuildPageAsync(
        int year,
        int quarter,
        ForecastFormViewModel form,
        CancellationToken cancellationToken)
    {
        var items = await _planning.GetForecastAsync(year, quarter, cancellationToken);
        var products = await _dbContext.Products.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

        return new ForecastPageViewModel
        {
            Year = year,
            Quarter = quarter,
            Items = items,
            Products = products,
            Form = form
        };
    }
}

/// <summary>Procedure v4.0 §6.3 — freeze periods managed on the calendar by the Release Manager.</summary>
[Authorize]
public sealed class FreezesController : Controller
{
    private readonly IPlanningService _planning;

    public FreezesController(IPlanningService planning)
    {
        _planning = planning;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _planning.GetFreezePeriodsAsync(includeInactive: true, cancellationToken);
        return View(new FreezePageViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanReviewRelease)]
    public async Task<IActionResult> Create(FreezeFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var items = await _planning.GetFreezePeriodsAsync(includeInactive: true, cancellationToken);
            return View("Index", new FreezePageViewModel { Items = items, Form = form });
        }

        try
        {
            await _planning.CreateFreezePeriodAsync(
                new CreateFreezePeriodRequest
                {
                    Name = form.Name,
                    FreezeType = form.FreezeType,
                    StartDateUtc = DateTime.SpecifyKind(form.StartDate, DateTimeKind.Local).ToUniversalTime(),
                    EndDateUtc = DateTime.SpecifyKind(form.EndDate, DateTimeKind.Local).ToUniversalTime(),
                    Authority = form.Authority,
                    Description = form.Description
                },
                cancellationToken);

            TempData["Success"] = "Freeze period created.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanReviewRelease)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _planning.DeactivateFreezePeriodAsync(id, cancellationToken);
            TempData["Success"] = "Freeze period deactivated.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or NotFoundException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Procedure v4.0 §7.3 — Post-Release Reviews.</summary>
[Authorize]
public sealed class ReviewsController : Controller
{
    private readonly IPostReleaseService _postRelease;

    public ReviewsController(IPostReleaseService postRelease)
    {
        _postRelease = postRelease;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _postRelease.ListReviewsAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var review = await _postRelease.GetReviewAsync(id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return View(review);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(UpdatePirRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReviewId, "Review updated.", () => _postRelease.UpdateReviewAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddAction(AddPirActionRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReviewId, "Corrective action added.", () => _postRelease.AddActionAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CompleteAction(Guid reviewId, Guid actionId, CancellationToken cancellationToken) =>
        RunAsync(reviewId, "Action completed.", () => _postRelease.CompleteActionAsync(reviewId, actionId, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(Guid reviewId, CancellationToken cancellationToken) =>
        RunAsync(reviewId, "Review completed.", () => _postRelease.CompleteReviewAsync(reviewId, cancellationToken));

    private async Task<IActionResult> RunAsync(Guid reviewId, string success, Func<Task> action)
    {
        try
        {
            await action();
            TempData["Success"] = success;
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or NotFoundException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = reviewId });
    }
}

/// <summary>Procedure v4.0 §9 — monthly KPIs for IT Governance review.</summary>
[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IGovernanceService _governance;
    private readonly IApplicationDbContext _dbContext;

    public ReportsController(IGovernanceService governance, IApplicationDbContext dbContext)
    {
        _governance = governance;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int months = 6, CancellationToken cancellationToken = default)
    {
        var kpis = await _governance.GetMonthlyKpisAsync(months, cancellationToken);

        var openReviews = await _dbContext.PostImplementationReviews.AsNoTracking()
            .CountAsync(item => item.Status != PirStatus.Completed, cancellationToken);
        var now = DateTime.UtcNow;
        var activeFreezes = await _dbContext.FreezePeriods.AsNoTracking()
            .CountAsync(item => item.IsActive && item.EndDate >= now, cancellationToken);
        var inReadiness = await _dbContext.Releases.AsNoTracking()
            .CountAsync(item => item.CurrentStatus == ReleaseStatus.ReadinessInProgress, cancellationToken);

        return View(new KpiPageViewModel
        {
            Months = kpis,
            OpenReviews = openReviews,
            ActiveFreezes = activeFreezes,
            ReleasesInReadiness = inReadiness
        });
    }
}
