using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization;

public interface IRolePermissionCommands
{
    Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task AssignMultiplePermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task RemoveMultiplePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
}

internal sealed class RolePermissionCommands : IRolePermissionCommands
{
    private readonly IWriteRepository<Role, Guid> _roleWriteRepository;
    private readonly IReadRepository<Role, Guid> _roleReadRepository;
    private readonly IReadRepository<Permission, Guid> _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RolePermissionCommands(
        IWriteRepository<Role, Guid> roleWriteRepository,
        IReadRepository<Role, Guid> roleReadRepository,
        IReadRepository<Permission, Guid> permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleWriteRepository = roleWriteRepository;
        _roleReadRepository = roleReadRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found");

        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission with ID {permissionId} not found");

        role.Grant(permission);
        _roleWriteRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found");

        role.Revoke(permissionId);
        _roleWriteRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignMultiplePermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found");

        var permissionIdsList = permissionIds.ToList();
        if (!permissionIdsList.Any()) return;

        var permissions = await _permissionRepository
            .ListAsync(p => permissionIdsList.Contains(p.Id), cancellationToken);

        if (permissions.Count != permissionIdsList.Count)
            throw new InvalidOperationException("One or more permissions not found");

        foreach (var permission in permissions)
        {
            role.Grant(permission);
        }

        _roleWriteRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMultiplePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found");

        var permissionIdsList = permissionIds.ToList();
        if (!permissionIdsList.Any()) return;

        foreach (var permissionId in permissionIdsList)
        {
            role.Revoke(permissionId);
        }

        _roleWriteRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found");

        var permissionIdsList = permissionIds.ToList();

        // Remove all existing permissions
        var existingPermissionIds = role.Permissions.Select(rp => rp.PermissionId).ToList();
        foreach (var permissionId in existingPermissionIds)
        {
            role.Revoke(permissionId);
        }

        // Add new permissions if any
        if (permissionIdsList.Any())
        {
            var permissions = await _permissionRepository
                .ListAsync(p => permissionIdsList.Contains(p.Id), cancellationToken);

            if (permissions.Count != permissionIdsList.Count)
                throw new InvalidOperationException("One or more permissions not found");

            foreach (var permission in permissions)
            {
                role.Grant(permission);
            }
        }

        _roleWriteRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}