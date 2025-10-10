using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Domain.Test.Users;

public class UsernameTests
{
    [Fact]
    public void Create_ValidUsername_ReturnsValueObject()
    {
        var username = Username.Create("  Alice  ");
        Assert.Equal("Alice", username.Value);
        Assert.Equal("Alice", username.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Create_Empty_Throws(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Username.Create(input));
        Assert.Equal("value", ex.ParamName);
    }

    [Theory]
    [InlineData("aa")]
    [InlineData("a")]
    public void Create_TooShort_Throws(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => Username.Create(input));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        var longName = new string('x', 51);
        var ex = Assert.Throws<ArgumentException>(() => Username.Create(longName));
        Assert.Equal("value", ex.ParamName);
    }
}

