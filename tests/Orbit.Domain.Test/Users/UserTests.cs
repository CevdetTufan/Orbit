using Orbit.Domain.Authorization;
using Orbit.Domain.Users;

namespace Orbit.Domain.Test.Users;

public class UserTests
{
    [Fact]
    public void Create_Valid_InitialState()
    {
        var user = User.Create("alice", "alice@example.com");
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("alice", user.Username.Value);
        Assert.Equal("alice@example.com", user.Email.Value);
        Assert.True(user.IsActive);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public void ActivateDeactivate_Toggles()
    {
        var user = User.Create("bob", "bob@example.com");
        user.Deactivate();
        Assert.False(user.IsActive);
        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void AssignRole_AddsOnceAndCreatesLink()
    {
        var user = User.Create("carol", "carol@example.com");
        var role = Role.Create("Manager");

        user.AssignRole(role);
        user.AssignRole(role); // idempotent

        Assert.Single(user.Roles);
        var link = user.Roles.First();
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(role.Id, link.RoleId);
        Assert.Equal(user, link.User);
        Assert.Equal(role, link.Role);
    }

    [Fact]
    public void RemoveRole_RemovesLink()
    {
        var user = User.Create("dave", "dave@example.com");
        var role = Role.Create("Staff");
        user.AssignRole(role);

        user.RemoveRole(role.Id);
        Assert.Empty(user.Roles);
    }
}

