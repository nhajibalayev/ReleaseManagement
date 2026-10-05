using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Authorization;

public sealed class ProductAccessService : IProductAccessService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ProductAccessService(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public bool SeesAllProducts =>
        _currentUser.IsInRole(RoleNames.Administrator) ||
        _currentUser.IsInRole(RoleNames.ReleaseManager) ||
        _currentUser.IsInRole(RoleNames.Auditor);

    public Task<IReadOnlyCollection<Guid>?> GetVisibleProductIdsAsync(CancellationToken cancellationToken = default) =>
        GetProductIdsWithAtLeastAsync(ProductAccessType.View, cancellationToken);

    public async Task<IReadOnlyCollection<Guid>?> GetProductIdsWithAtLeastAsync(
        ProductAccessType minimum,
        CancellationToken cancellationToken = default)
    {
        if (SeesAllProducts)
        {
            return null;
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Array.Empty<Guid>();
        }

        var userId = _currentUser.UserId;
        var ids = await _dbContext.UserProductAccesses
            .AsNoTracking()
            .Where(access => access.UserId == userId && access.AccessType >= minimum)
            .Select(access => access.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids;
    }

    public async Task<ProductAccessType?> GetAccessTypeAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            return ProductAccessType.Manage;
        }

        if (SeesAllProducts)
        {
            return ProductAccessType.View;
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return null;
        }

        var userId = _currentUser.UserId;
        return await _dbContext.UserProductAccesses
            .AsNoTracking()
            .Where(access => access.UserId == userId && access.ProductId == productId)
            .Select(access => (ProductAccessType?)access.AccessType)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
