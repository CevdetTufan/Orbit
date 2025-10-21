using Orbit.Domain.Authorization;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Authorization;

public interface IRoleCommands
{
	Task<Guid> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
	Task UpdateAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default);
	Task<bool> DeleteIfNoUsersAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class RoleCommands : IRoleCommands
{
	private readonly IRoleRepository _roleRepository;
	private readonly IRepository<User, Guid> _usersRead;
	private readonly RolePermissionDomainService _domainService;
	private readonly IUnitOfWork _unitOfWork;

	public RoleCommands(
		IRoleRepository roleRepository,
		IRepository<Domain.Users.User, Guid> usersRead,
		RolePermissionDomainService domainService,
		IUnitOfWork unitOfWork)
	{
		_roleRepository = roleRepository;
		_usersRead = usersRead;
		_domainService = domainService;
		_unitOfWork = unitOfWork;
	}

	public async Task<Guid> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
	{
		var role = Role.Create(name, description);
		await _roleRepository.AddAsync(role, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return role.Id;
	}

	public async Task UpdateAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default)
	{
		var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
		if (role is null)
			throw new InvalidOperationException("Role not found");

		role.Rename(name);
		role.UpdateDescription(description);
		_roleRepository.Update(role);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task<bool> DeleteIfNoUsersAsync(Guid id, CancellationToken cancellationToken = default)
	{
		// 1. Business Rule: Check if any users have this role assigned
		var hasUsers = await _usersRead.AnyAsync(u => u.Roles.Any(ur => ur.RoleId == id), cancellationToken);
		if (hasUsers)
		{
			throw new AuthorizationDomainException(
				"Role cannot be deleted because it is assigned to one or more users. " +
				"Please unassign the role from all users before deleting.");
		}

		// 2. Get role with permissions to validate business rules
		var role = await _roleRepository.GetWithPermissionsAsync(id, cancellationToken);
		if (role is null)
		{
			return true; // Already deleted (idempotent)
		}

		// 3. Business Rule: Check if role has any permissions assigned
		// This throws AuthorizationDomainException with clear message if validation fails
		_domainService.EnsureRoleCanBeDeleted(role);

		// 4. Role passed all validations, safe to delete
		_roleRepository.Remove(role);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		
		return true;
	}
}


