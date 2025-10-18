using Orbit.Application.Authorization.Models;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization.Specifications;

public class PermissionBasicSpec : BaseSpecification<Permission, PermissionDto>
{
    public PermissionBasicSpec()
    {
        ApplySelector(p => new PermissionDto(
            p.Id,
            p.Code,
            p.Description
        ));

        DisableTracking();
        ApplyOrderBy(p => p.Code);
    }
}