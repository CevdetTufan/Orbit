using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users.Specifications;

public sealed class UserByUsernameWithRolesSpec : BaseSpecification<User>
{
    public UserByUsernameWithRolesSpec(string username)
        : base(u => u.Username.Value == username)
    {
        AddInclude(u => u.Roles);
    }
}

