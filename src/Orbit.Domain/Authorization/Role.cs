using Orbit.Domain.Common;

namespace Orbit.Domain.Authorization;

public sealed class Role : Entity<Guid>, IAggregateRoot
{
    private readonly List<RolePermission> _permissions = new();

    public string Name { get; private set; } = null!; 
    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    private Role(Guid id, string name, string? description) : base(id)
    {
        Name = ValidateName(name);
        Description = description?.Trim();
    }

    public static Role Create(string name, string? description = null)
        => new(Guid.NewGuid(), name, description);

    public void Rename(string name) => Name = ValidateName(name);
    public void UpdateDescription(string? description) => Description = description?.Trim();

    /// <summary>
    /// Checks if the role has a specific permission.
    /// Used by domain services for business rule validation.
    /// </summary>
    public bool HasPermission(Guid permissionId)
        => _permissions.Any(x => x.PermissionId == permissionId);

    public void Grant(Permission permission)
    {
        if (_permissions.Any(x => x.PermissionId == permission.Id))
            return; 
        _permissions.Add(RolePermission.Create(this, permission));
    }

    public void Revoke(Guid permissionId)
    {
        var link = _permissions.FirstOrDefault(x => x.PermissionId == permissionId);
        if (link is null) return;
        _permissions.Remove(link);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required", nameof(name));
        var trimmed = name.Trim();
        if (trimmed.Length is < 2 or > 100)
            throw new ArgumentException("Role name length must be 2-100 characters", nameof(name));
        return trimmed;
    }
}
