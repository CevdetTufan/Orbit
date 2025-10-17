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
	private readonly IReadRepository<Role, Guid> _roles;
	private readonly IReadRepository<Domain.Users.User, Guid> _users;

	public RoleQueries(IReadRepository<Role, Guid> roles, IReadRepository<Domain.Users.User, Guid> users)
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
