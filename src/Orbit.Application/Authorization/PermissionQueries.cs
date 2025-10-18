using Orbit.Application.Authorization.Models;
using Orbit.Application.Authorization.Specifications;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization;

public interface IPermissionQueries
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
}

internal sealed class PermissionQueries : IPermissionQueries
{
    private readonly IReadRepository<Permission, Guid> _permissionRepository;
    private readonly IReadRepository<Role, Guid> _roleRepository;

    public PermissionQueries(
        IReadRepository<Permission, Guid> permissionRepository,
        IReadRepository<Role, Guid> roleRepository)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var spec = new PermissionBasicSpec();
        return await _permissionRepository.ListAsync(spec, cancellationToken);
    }

    public async Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var roleSpec = new RoleWithPermissionsSpec(roleId);
        var role = await _roleRepository.FirstOrDefaultAsync(roleSpec, cancellationToken);
        
        if (role == null)
            return null;

        // Get assigned permissions for this role
        var assignedPermissions = new List<PermissionDto>();
        
        // Get all permissions to show available ones
        var allPermissions = await GetAllAsync(cancellationToken);
        var assignedPermissionIds = role.Permissions.Select(rp => rp.PermissionId).ToHashSet();
        
        // Find assigned permissions from all permissions
        var assignedPermissionsList = allPermissions.Where(p => assignedPermissionIds.Contains(p.Id)).ToList();
        var availablePermissions = allPermissions.Where(p => !assignedPermissionIds.Contains(p.Id)).ToList();

        return new RoleWithPermissionsDto(
            role.Id,
            role.Name,
            role.Description,
            assignedPermissionsList,
            availablePermissions
        );
    }
}