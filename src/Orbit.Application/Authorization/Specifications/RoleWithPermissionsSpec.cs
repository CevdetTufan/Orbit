using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Application.Authorization.Specifications;

public class RoleWithPermissionsSpec : BaseSpecification<Role, Role>
{
    public RoleWithPermissionsSpec(Guid roleId) : base(r => r.Id == roleId)
    {
        AddInclude(r => r.Permissions);
        
        // Enable tracking for write operations (entity updates)
        EnableTracking();
    }
}