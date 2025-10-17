using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization;

public interface IRoleCommands
{
	Task<Guid> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
	Task UpdateAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default);
	Task<bool> DeleteIfNoUsersAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class RoleCommands : IRoleCommands
{
	private readonly IWriteRepository<Role, Guid> _rolesWrite;
	private readonly IReadRepository<Role, Guid> _rolesRead;
	private readonly IReadRepository<Domain.Users.User, Guid> _usersRead;
	private readonly IUnitOfWork _unitOfWork;

	public RoleCommands(
		IWriteRepository<Role, Guid> rolesWrite,
		IReadRepository<Role, Guid> rolesRead,
		IReadRepository<Domain.Users.User, Guid> usersRead,
		IUnitOfWork unitOfWork)
	{
		_rolesWrite = rolesWrite;
		_rolesRead = rolesRead;
		_usersRead = usersRead;
		_unitOfWork = unitOfWork;
	}

	public async Task<Guid> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
	{
		var role = Role.Create(name, description);
		await _rolesWrite.AddAsync(role, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return role.Id;
	}

	public async Task UpdateAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default)
	{
		var role = await _rolesRead.GetByIdAsync(id, cancellationToken);
		if (role is null)
			throw new InvalidOperationException("Role not found");

		role.Rename(name);
		role.UpdateDescription(description);
		_rolesWrite.Update(role);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task<bool> DeleteIfNoUsersAsync(Guid id, CancellationToken cancellationToken = default)
	{
		var hasUsers = await _usersRead.AnyAsync(u => u.Roles.Any(ur => ur.RoleId == id), cancellationToken);
		if (hasUsers)
			return false;

		var role = await _rolesRead.GetByIdAsync(id, cancellationToken);
		if (role is null)
			return true; // already gone
		_rolesWrite.Remove(role);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return true;
	}
}


