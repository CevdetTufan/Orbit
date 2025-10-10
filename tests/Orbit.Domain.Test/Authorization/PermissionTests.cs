using Orbit.Domain.Authorization;

namespace Orbit.Domain.Test.Authorization;

public class PermissionTests
{
    [Fact]
    public void Create_Valid_SetsProperties()
    {
        var p = Permission.Create("  manage.users  ", "  Can manage users  ");
        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.Equal("manage.users", p.Code);
        Assert.Equal("Can manage users", p.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_InvalidCode_Throws(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() => Permission.Create(code));
        Assert.Equal("code", ex.ParamName);
    }

    [Fact]
    public void Create_CodeTooLong_Throws()
    {
        var longCode = new string('a', 201);
        var ex = Assert.Throws<ArgumentException>(() => Permission.Create(longCode));
        Assert.Equal("code", ex.ParamName);
    }

    [Fact]
    public void Rename_UpdatesAndTrims()
    {
        var p = Permission.Create("code");
        p.Rename("  new.code  ");
        Assert.Equal("new.code", p.Code);
    }

    [Fact]
    public void UpdateDescription_AllowsNullAndTrims()
    {
        var p = Permission.Create("code", "  desc  ");
        p.UpdateDescription("  updated  ");
        Assert.Equal("updated", p.Description);
        p.UpdateDescription(null);
        Assert.Null(p.Description);
    }
}

