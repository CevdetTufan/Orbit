using Orbit.Application.Authorization.Models;
using Orbit.Application.Authorization.Specifications;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization;

public interface IRoleQueries
{
	Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

internal sealed class RoleQueries : IRoleQueries
{
	private readonly IRepository<Role, Guid> _roles;
	private readonly IRepository<Domain.Users.User, Guid> _users;

	public RoleQueries(IRepository<Role, Guid> roles, IRepository<Domain.Users.User, Guid> users)
	{
		_roles = roles;
		_users = users;
	}

	public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var spec = new RoleWithCanDeleteSpec();
		return await _roles.ListAsync(spec, cancellationToken);
	}
}
