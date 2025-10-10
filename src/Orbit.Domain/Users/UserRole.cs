using Orbit.Domain.Authorization;
using Orbit.Domain.Common;

namespace Orbit.Domain.Users;

public sealed class UserRole : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() { }

    private UserRole(Guid id, Guid userId, Guid roleId) : base(id)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public static UserRole Create(User user, Role role)
        => new(Guid.NewGuid(), user.Id, role.Id)
        {
            User = user,
            Role = role
        };
}

