using Orbit.Application.Users.Models;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users.Specifications;

internal sealed class UsersPagedSpec : BaseSpecification<User, UserListItemDto>
{
    public UsersPagedSpec(int pageIndex, int pageSize, string? searchQuery = null)
        : base(u => string.IsNullOrWhiteSpace(searchQuery) ||
                    u.Username.Value.Contains(searchQuery) ||
                    u.Email.Value.Contains(searchQuery))
    {
        AddInclude(u => u.Roles);
        
        ApplyPaging(pageIndex * pageSize, pageSize);
        
        Selector = u => new UserListItemDto(
            u.Id,
            u.Username.Value,
            u.Email.Value,
            u.IsActive,
            u.Roles.Select(ur => ur.Role.Name).ToList()
        );
    }
}
