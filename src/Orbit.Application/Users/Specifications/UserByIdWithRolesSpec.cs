using Orbit.Application.Users.Models;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users.Specifications;

internal sealed class UserByIdWithRolesSpec : BaseSpecification<User, UserDetailDto>
{
    public UserByIdWithRolesSpec(Guid userId)
        : base(u => u.Id == userId)
    {
        AddInclude(u => u.Roles);
        
        Selector = u => new UserDetailDto(
            u.Id,
            u.Username.Value,
            u.Email.Value,
            u.IsActive,
            u.Roles.Select(ur => new RoleInfo(ur.RoleId, ur.Role.Name)).ToList()
        );
    }
}
