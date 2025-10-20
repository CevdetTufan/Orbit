using Orbit.Domain.Common;

namespace Orbit.Domain.Users.Specifications;

/// <summary>
/// Specification for fetching a User with its UserRoles and related Roles.
/// This is required for role assignment operations to avoid concurrency issues.
/// </summary>
public sealed class UserWithRolesSpecification : BaseSpecification<User>
{
    public UserWithRolesSpecification(Guid userId) 
        : base(u => u.Id == userId)
    {
        // Include UserRoles collection and nested Role navigation property
        // Using string-based include to support ThenInclude syntax
        AddInclude("Roles.Role");
        
        // Enable tracking for write operations (entity updates)
        EnableTracking();
    }
}
