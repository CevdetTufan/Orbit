using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users.Specifications;

public sealed class UsersByQuerySpec : BaseSpecification<User>
{
    public UsersByQuerySpec(string query)
        : base(u => u.Username.Value.Contains(query) || u.Email.Value.Contains(query))
    {
        ApplyOrderBy(u => u.Username.Value);
    }
}

