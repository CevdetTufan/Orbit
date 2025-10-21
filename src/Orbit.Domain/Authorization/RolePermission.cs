using Orbit.Domain.Common;

namespace Orbit.Domain.Authorization;

public sealed class RolePermission : Entity<Guid>, IAggregateRoot
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { }

    private RolePermission(Guid id, Guid roleId, Guid permissionId) : base(id)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public static RolePermission Create(Role role, Permission permission)
        => new(Guid.NewGuid(), role.Id, permission.Id)
        {
            Role = role,
            Permission = permission
        };
}

