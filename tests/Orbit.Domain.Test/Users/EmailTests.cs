using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Domain.Test.Users;

public class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ReturnsValueObject()
    {
        var email = Email.Create("  user@example.com  ");
        Assert.Equal("user@example.com", email.Value);
        Assert.Equal("user@example.com", email.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Create_Empty_Throws(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Email.Create(input));
        Assert.Equal("value", ex.ParamName);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid")]
    [InlineData("a@b")]
    public void Create_InvalidFormat_Throws(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Email.Create(input));
        Assert.Equal("value", ex.ParamName);
    }
}

