using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users.Specifications;

internal sealed class UsersCountSpec : BaseSpecification<User>
{
    public UsersCountSpec(string? searchQuery = null)
        : base(u => string.IsNullOrWhiteSpace(searchQuery) ||
                    u.Username.Value.Contains(searchQuery) ||
                    u.Email.Value.Contains(searchQuery))
    {
    }
}
