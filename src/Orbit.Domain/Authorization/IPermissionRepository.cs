using Orbit.Domain.Common;

namespace Orbit.Domain.Authorization;

/// <summary>
/// Domain-specific repository interface for Permission aggregate.
/// Provides permission-specific query operations following DDD principles.
/// Note: Permissions are typically read-only in most scenarios, but extends IRepository for consistency.
/// </summary>
public interface IPermissionRepository : IRepository<Permission, Guid>
{
    /// <summary>
    /// Gets a permission by its code for business logic operations.
    /// </summary>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a permission with the given code already exists.
    /// Used for domain rule enforcement.
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets multiple permissions by their IDs for batch operations.
    /// Returns untracked entities for read-only scenarios.
    /// </summary>
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
}