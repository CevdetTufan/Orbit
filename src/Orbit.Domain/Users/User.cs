using Orbit.Domain.Common;
using Orbit.Domain.Users.ValueObjects;
using Orbit.Domain.Authorization;

namespace Orbit.Domain.Users;

public sealed class User : Entity<Guid>, IAggregateRoot
{
    private readonly List<UserRole> _roles = new();

    public Username Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private User() { }

    private User(Guid id, Username username, Email email) : base(id)
    {
        Username = username;
        Email = email;
    }

    public static User Create(string username, string email)
    {
        return new User(Guid.NewGuid(), Username.Create(username), Email.Create(email));
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void AssignRole(Role role)
    {
        if (_roles.Any(x => x.RoleId == role.Id))
            return;

        _roles.Add(UserRole.Create(this, role));
    }

    public void RemoveRole(Guid roleId)
    {
        var link = _roles.FirstOrDefault(x => x.RoleId == roleId);
        if (link is null) return;
        _roles.Remove(link);
    }

    public void UpdateEmail(Email email)
    {
        Email = email;
    }
}
