namespace Orbit.Domain.Authorization;

/// <summary>
/// Domain service for role-permission business operations.
/// Encapsulates complex business rules that involve multiple aggregates.
/// </summary>
public class RolePermissionDomainService
{
    /// <summary>
    /// Validates if a permission can be assigned to a role.
    /// Encapsulates business rules for permission assignment.
    /// </summary>
    public bool CanAssignPermission(Role role, Permission permission)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        if (permission == null) throw new ArgumentNullException(nameof(permission));
        
        // Business rule: Permission cannot be assigned twice
        return !role.HasPermission(permission.Id);
    }
    
    /// <summary>
    /// Validates if a permission can be removed from a role.
    /// </summary>
    public bool CanRemovePermission(Role role, Guid permissionId)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        
        // Business rule: Can only remove existing permissions
        return role.HasPermission(permissionId);
    }
    
    /// <summary>
    /// Validates if multiple permissions can be assigned to a role.
    /// </summary>
    public ValidationResult ValidatePermissionAssignment(Role role, IEnumerable<Permission> permissions)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        if (permissions == null) throw new ArgumentNullException(nameof(permissions));
        
        var permissionsList = permissions.ToList();
        var errors = new List<string>();
        
        foreach (var permission in permissionsList)
        {
            if (!CanAssignPermission(role, permission))
            {
                errors.Add($"Permission '{permission.Code}' is already assigned to role '{role.Name}'");
            }
        }
        
        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates if a role can be deleted.
    /// Business Rule: A role cannot be deleted if it has any permissions assigned.
    /// </summary>
    public bool CanDeleteRole(Role role)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        
        // Business rule: Role can only be deleted if it has no permissions
        return !role.Permissions.Any();
    }

    /// <summary>
    /// Validates if a role can be deleted and throws a domain exception if not.
    /// This enforces the business rule at the domain level.
    /// </summary>
    public void EnsureRoleCanBeDeleted(Role role)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        
        if (!CanDeleteRole(role))
        {
            throw new AuthorizationDomainException(
                $"Role '{role.Name}' cannot be deleted because it has {role.Permissions.Count} permission(s) assigned. " +
                "Please remove all permissions before deleting the role.");
        }
    }
}

/// <summary>
/// Represents validation result for domain operations.
/// </summary>
public class ValidationResult
{
    public IReadOnlyList<string> Errors { get; }
    public bool IsValid => !Errors.Any();
    
    public ValidationResult(IEnumerable<string> errors)
    {
        Errors = errors?.ToList() ?? new List<string>();
    }
    
    public static ValidationResult Success() => new(Enumerable.Empty<string>());
}