using Orbit.Domain.Authorization;

namespace Orbit.Domain.Test.Authorization;

public class RoleTests
{
    [Fact]
    public void Create_Valid_SetsProperties()
    {
        var role = Role.Create("  Admin  ", "  All access  ");
        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Admin", role.Name);
        Assert.Equal("All access", role.Description);
        Assert.Empty(role.Permissions);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_InvalidName_Throws(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => Role.Create(name));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Create_NameTooShortOrLong_Throws()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => Role.Create("a")).ParamName);
        var longName = new string('x', 101);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => Role.Create(longName)).ParamName);
    }

    [Fact]
    public void Rename_UpdatesAndTrims()
    {
        var role = Role.Create("Admin");
        role.Rename("  Super  ");
        Assert.Equal("Super", role.Name);
    }

    [Fact]
    public void UpdateDescription_TrimsAndAllowsNull()
    {
        var role = Role.Create("Admin", "  desc  ");
        role.UpdateDescription("  updated  ");
        Assert.Equal("updated", role.Description);
        role.UpdateDescription(null);
        Assert.Null(role.Description);
    }

    [Fact]
    public void Grant_AddsOnceAndCreatesLink()
    {
        var role = Role.Create("Admin");
        var p = Permission.Create("perm");

        role.Grant(p);
        role.Grant(p); // idempotent

        Assert.Single(role.Permissions);
        var link = role.Permissions.First();
        Assert.Equal(role.Id, link.RoleId);
        Assert.Equal(p.Id, link.PermissionId);
        Assert.Equal(role, link.Role);
        Assert.Equal(p, link.Permission);
    }

    [Fact]
    public void Revoke_RemovesLink()
    {
        var role = Role.Create("Admin");
        var p = Permission.Create("perm");
        role.Grant(p);

        role.Revoke(p.Id);

        Assert.Empty(role.Permissions);
    }
}

