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
	private readonly IRepository<Role, Guid> _roleRepository;
	private readonly IRepository<Permission, Guid> _permissionRepository;
	private readonly IRepository<RolePermission, Guid> _rolePermissionRepository;
	private readonly IUnitOfWork _unitOfWork;

	public RolePermissionCommands(
		IRepository<Role, Guid> roleRepository,
		IRepository<Permission, Guid> permissionRepository,
		IRepository<RolePermission, Guid> rolePermissionRepository,
		IUnitOfWork unitOfWork)
	{
		_roleRepository = roleRepository;
		_permissionRepository = permissionRepository;
		_rolePermissionRepository = rolePermissionRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
	{
		var role = await GetTrackedRoleAsync(roleId, cancellationToken);
		// if already assigned, nothing to do
		if (role.Permissions.Any(rp => rp.PermissionId == permissionId))
			return;

		var permission = await GetTrackedPermissionAsync(permissionId, cancellationToken);

		// Create join entity and add directly to repository so EF treats it as Added
		var link = RolePermission.Create(role, permission);
		await _rolePermissionRepository.AddAsync(link, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
	{
		var role = await GetTrackedRoleAsync(roleId, cancellationToken);

		var link = role.Permissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
		if (link != null)
		{
			// remove via repository so EF marks as Deleted
			_rolePermissionRepository.Remove(link);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			return;
		}

		// If not loaded on role, try to fetch the link directly and remove
		var links = await _rolePermissionRepository.ListAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
		var found = links.FirstOrDefault();
		if (found != null)
		{
			_rolePermissionRepository.Remove(found);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
		}
	}

	public async Task AssignMultiplePermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
	{
		var permissionIdsList = permissionIds.ToList();
		if (!permissionIdsList.Any()) return;

		var role = await GetTrackedRoleAsync(roleId, cancellationToken);
		var permissions = await GetTrackedPermissionsAsync(permissionIdsList, cancellationToken);

		var permissionsToGrant = permissions
			.Where(permission => !role.Permissions.Any(rp => rp.PermissionId == permission.Id))
			.ToList();

		foreach (var p in permissionsToGrant)
		{
			var link = RolePermission.Create(role, p);
			await _rolePermissionRepository.AddAsync(link, cancellationToken);
		}

		if (permissionsToGrant.Any())
			await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveMultiplePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
	{
		var permissionIdsList = permissionIds.ToList();
		if (!permissionIdsList.Any()) return;

		var role = await GetTrackedRoleAsync(roleId, cancellationToken);

		var permissionIdsToRevoke = permissionIdsList
			.Where(permissionId => role.Permissions.Any(rp => rp.PermissionId == permissionId))
			.ToList();

		if (permissionIdsToRevoke.Any())
		{
			foreach (var id in permissionIdsToRevoke)
			{
				var link = role.Permissions.First(rp => rp.PermissionId == id);
				_rolePermissionRepository.Remove(link);
			}
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			return;
		}

		// If none loaded, delete by query
		var links = await _rolePermissionRepository.ListAsync(rp => rp.RoleId == roleId && permissionIdsList.Contains(rp.PermissionId), cancellationToken);
		foreach (var l in links)
		{
			_rolePermissionRepository.Remove(l);
		}
		if (links.Any())
			await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
	{
		var permissionIdsList = permissionIds.ToList();

		var role = await GetTrackedRoleAsync(roleId, cancellationToken);

		// Remove existing links
		var existingLinks = role.Permissions.ToList();
		foreach (var l in existingLinks)
		{
			_rolePermissionRepository.Remove(l);
		}

		// Add new ones
		if (permissionIdsList.Any())
		{
			var permissions = await GetTrackedPermissionsAsync(permissionIdsList, cancellationToken);
			foreach (var p in permissions)
			{
				var link = RolePermission.Create(role, p);
				await _rolePermissionRepository.AddAsync(link, cancellationToken);
			}
		}

		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	// Helper methods
	private async Task<Role> GetTrackedRoleAsync(Guid roleId, CancellationToken cancellationToken)
	{
		return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
				?? throw new InvalidOperationException($"Role with ID {roleId} not found");
	}

	private async Task<Permission> GetTrackedPermissionAsync(Guid permissionId, CancellationToken cancellationToken)
	{
		return await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
			?? throw new InvalidOperationException($"Permission with ID {permissionId} not found");
	}

	private async Task<IReadOnlyList<Permission>> GetTrackedPermissionsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
	{
		var permissions = await _permissionRepository.ListAsync(p => permissionIds.Contains(p.Id), cancellationToken);

		if (permissions.Count != permissionIds.Count())
			throw new InvalidOperationException("One or more permissions not found");

		return permissions;
	}
}