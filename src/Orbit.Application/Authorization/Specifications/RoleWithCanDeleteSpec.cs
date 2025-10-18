using Orbit.Application.Authorization.Models;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization.Specifications;
public class RoleWithCanDeleteSpec : BaseSpecification<Role, RoleDto>
{
	public RoleWithCanDeleteSpec()
	{
		Selector = r => new RoleDto(
			r.Id,
			r.Name,
			r.Description,
			!r.Permissions.Any()
		);

		DisableTracking();
	}
}
