using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Authorization;

/// <summary>
/// Product-scoped visibility. Regular users see only the products they are members of
/// (<c>UserProductAccess</c>); Administrator, ReleaseManager and Auditor see every product.
/// </summary>
public interface IProductAccessService
{
    /// <summary>True for roles that are not restricted by product membership.</summary>
    bool SeesAllProducts { get; }

    /// <summary>Product ids visible to the current user, or <c>null</c> when every product is visible.</summary>
    Task<IReadOnlyCollection<Guid>?> GetVisibleProductIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Product ids where the current user has at least <paramref name="minimum"/> access (null = all products).</summary>
    Task<IReadOnlyCollection<Guid>?> GetProductIdsWithAtLeastAsync(
        ProductAccessType minimum,
        CancellationToken cancellationToken = default);

    /// <summary>Access level of the current user on one product; <c>null</c> when not a member.</summary>
    Task<ProductAccessType?> GetAccessTypeAsync(Guid productId, CancellationToken cancellationToken = default);
}
