using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
[Route("api/internal")]
public sealed class InternalApiController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;

    public InternalApiController(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("services")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateRelease)]
    public async Task<IActionResult> GetServicesByProduct(Guid productId, CancellationToken cancellationToken)
    {
        var services = await _dbContext.Services.AsNoTracking()
            .Where(item => item.ProductId == productId && item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.Code })
            .ToListAsync(cancellationToken);

        return Ok(services);
    }

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> DashboardStats(CancellationToken cancellationToken)
    {
        var total = await _dbContext.Releases.AsNoTracking().CountAsync(cancellationToken);
        var draft = await _dbContext.Releases.AsNoTracking()
            .CountAsync(item => item.CurrentStatus == Domain.Enums.ReleaseStatus.Draft, cancellationToken);
        return Ok(new { total, draft });
    }
}
