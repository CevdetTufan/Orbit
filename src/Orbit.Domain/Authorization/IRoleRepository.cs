using Orbit.Domain.Common;

namespace Orbit.Domain.Authorization;

/// <summary>
/// Domain-specific repository interface for Role aggregate.
/// Follows DDD principles by providing domain-meaningful operations.
/// Extends IRepository which combines read and write operations.
/// </summary>
public interface IRoleRepository : IRepository<Role, Guid>
{
    /// <summary>
    /// Gets a role with its permissions loaded for update operations.
    /// Returns tracked entity for aggregate consistency.
    /// </summary>
    Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a role by name for business rule validation.
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a role with the given name already exists.
    /// Used for domain rule enforcement.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}