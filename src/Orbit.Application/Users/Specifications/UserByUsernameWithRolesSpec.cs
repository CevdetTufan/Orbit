using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Application.Users.Specifications;

public sealed class UserByUsernameWithRolesSpec : BaseSpecification<User>
{
    public UserByUsernameWithRolesSpec(string username)
        : base(u => u.Username == Username.Create(username))
    {
        AddInclude(u => u.Roles);
    }
}
