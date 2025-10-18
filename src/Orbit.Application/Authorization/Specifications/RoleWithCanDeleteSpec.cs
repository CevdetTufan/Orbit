using Orbit.Application.Authorization.Models;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization.Specifications;
public class RoleWithCanDeleteSpec : BaseSpecification<Role, RoleDto>
{
	public RoleWithCanDeleteSpec()
	{
		// Sadece gerekli kolonları seçiyoruz (SELECT * yerine projection)
		Selector = r => new RoleDto(
			r.Id,
			r.Name,
			r.Description,
			// Her rol silinebilir - business logic başka yerde kontrol edilecek
			true
		);

		DisableTracking();
	}
}
