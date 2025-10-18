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
    private readonly IReadRepository<Role, Guid> _roleReadRepository;
    private readonly IWriteRepository<Role, Guid> _roleWriteRepository;
    private readonly IReadRepository<Permission, Guid> _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RolePermissionCommands(
        IReadRepository<Role, Guid> roleReadRepository,
        IWriteRepository<Role, Guid> roleWriteRepository,
        IReadRepository<Permission, Guid> permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleReadRepository = roleReadRepository;
        _roleWriteRepository = roleWriteRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tracked entities al - Update için gerekli
            var role = await GetTrackedRoleAsync(roleId, cancellationToken);
            
            // Permission kontrolü için untracked yeterli
            var permission = await GetUntrackedPermissionAsync(permissionId, cancellationToken);

            // Duplicate kontrolü
            if (role.Permissions.Any(rp => rp.PermissionId == permissionId))
            {
                return; // Zaten atanmýþ
            }

            role.Grant(permission);
            // Update gereksiz - tracked entity otomatik algýlanýr
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshRole = await GetTrackedRoleAsync(roleId, cancellationToken);
                var freshPermission = await GetUntrackedPermissionAsync(permissionId, cancellationToken);

                if (!freshRole.Permissions.Any(rp => rp.PermissionId == permissionId))
                {
                    freshRole.Grant(freshPermission);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            });
        }
    }

    public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await GetTrackedRoleAsync(roleId, cancellationToken);

            // Permission'ýn mevcut olup olmadýðýný kontrol et
            if (!role.Permissions.Any(rp => rp.PermissionId == permissionId))
            {
                return; // Zaten yok
            }

            role.Revoke(permissionId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshRole = await GetTrackedRoleAsync(roleId, cancellationToken);

                if (freshRole.Permissions.Any(rp => rp.PermissionId == permissionId))
                {
                    freshRole.Revoke(permissionId);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            });
        }
    }

    public async Task AssignMultiplePermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var permissionIdsList = permissionIds.ToList();
        if (!permissionIdsList.Any()) return;

        try
        {
            var role = await GetTrackedRoleAsync(roleId, cancellationToken);
            var permissions = await GetUntrackedPermissionsAsync(permissionIdsList, cancellationToken);

            var permissionsToGrant = permissions
                .Where(permission => !role.Permissions.Any(rp => rp.PermissionId == permission.Id))
                .ToList();

            if (permissionsToGrant.Any())
            {
                permissionsToGrant.ForEach(role.Grant);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshRole = await GetTrackedRoleAsync(roleId, cancellationToken);
                var freshPermissions = await GetUntrackedPermissionsAsync(permissionIdsList, cancellationToken);

                var permissionsToGrant = freshPermissions
                    .Where(permission => !freshRole.Permissions.Any(rp => rp.PermissionId == permission.Id))
                    .ToList();

                if (permissionsToGrant.Any())
                {
                    permissionsToGrant.ForEach(freshRole.Grant);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            });
        }
    }

    public async Task RemoveMultiplePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var permissionIdsList = permissionIds.ToList();
        if (!permissionIdsList.Any()) return;

        try
        {
            var role = await GetTrackedRoleAsync(roleId, cancellationToken);

            var permissionIdsToRevoke = permissionIdsList
                .Where(permissionId => role.Permissions.Any(rp => rp.PermissionId == permissionId))
                .ToList();

            if (permissionIdsToRevoke.Any())
            {
                permissionIdsToRevoke.ForEach(role.Revoke);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshRole = await GetTrackedRoleAsync(roleId, cancellationToken);

                var permissionIdsToRevoke = permissionIdsList
                    .Where(permissionId => freshRole.Permissions.Any(rp => rp.PermissionId == permissionId))
                    .ToList();

                if (permissionIdsToRevoke.Any())
                {
                    permissionIdsToRevoke.ForEach(freshRole.Revoke);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            });
        }
    }

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var permissionIdsList = permissionIds.ToList();

        try
        {
            var role = await GetTrackedRoleAsync(roleId, cancellationToken);

            // Remove all existing permissions
            var existingPermissionIds = role.Permissions.Select(rp => rp.PermissionId).ToList();
            existingPermissionIds.ForEach(role.Revoke);

            // Add new permissions if any
            if (permissionIdsList.Any())
            {
                var permissions = await GetUntrackedPermissionsAsync(permissionIdsList, cancellationToken);
                permissions.ToList().ForEach(role.Grant);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshRole = await GetTrackedRoleAsync(roleId, cancellationToken);

                // Remove all existing permissions
                var existingPermissionIds = freshRole.Permissions.Select(rp => rp.PermissionId).ToList();
                existingPermissionIds.ForEach(freshRole.Revoke);

                // Add new permissions if any
                if (permissionIdsList.Any())
                {
                    var permissions = await GetUntrackedPermissionsAsync(permissionIdsList, cancellationToken);
                    permissions.ToList().ForEach(freshRole.Grant);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    // Helper methods
    private async Task<Role> GetTrackedRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        // GetByIdAsync ile tracked entity al (Update için gerekli)
        return await _roleReadRepository.GetByIdAsync(roleId, cancellationToken)
               ?? throw new InvalidOperationException($"Role with ID {roleId} not found");
    }

    private async Task<Permission> GetUntrackedPermissionAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        // Permission sadece okuma için kullanýlýyor, untracked yeterli
        var permissions = await _permissionRepository.ListAsync(p => p.Id == permissionId, cancellationToken);
        return permissions.FirstOrDefault() ?? throw new InvalidOperationException($"Permission with ID {permissionId} not found");
    }

    private async Task<IReadOnlyList<Permission>> GetUntrackedPermissionsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        // Permissions sadece okuma için kullanýlýyor, untracked yeterli
        var permissions = await _permissionRepository.ListAsync(p => permissionIds.Contains(p.Id), cancellationToken);
        
        if (permissions.Count != permissionIds.Count())
            throw new InvalidOperationException("One or more permissions not found");
            
        return permissions;
    }

    private static async Task HandleConcurrencyAndRetryAsync(Func<Task> retryOperation)
    {
        try
        {
            await retryOperation();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new InvalidOperationException(
                "The operation could not be completed due to concurrent modifications. Please refresh and try again.",
                ex);
        }
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        return ex.Message.Contains("database operation was expected to affect") ||
               ex.Message.Contains("concurrency") ||
               ex.Message.Contains("cannot be tracked because another instance") ||
               ex.Message.Contains("is already being tracked") ||
               ex.GetType().Name.Contains("Concurrency") ||
               ex.GetType().Name.Contains("DbUpdateConcurrency") ||
               ex.GetType().Name.Contains("InvalidOperation");
    }
}