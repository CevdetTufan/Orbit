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
		var roles = await _roles.ListAsync(cancellationToken: cancellationToken);
		var list = new List<RoleDto>(roles.Count);
		foreach (var r in roles)
		{
			var anyUserHasRole = await _users.AnyAsync(u => u.Roles.Any(ur => ur.RoleId == r.Id), cancellationToken);
			list.Add(new RoleDto(r.Id, r.Name, r.Description, CanDelete: !anyUserHasRole));
		}
		return list;
	}
}

public sealed record RoleDto(Guid Id, string Name, string? Description, bool CanDelete);


