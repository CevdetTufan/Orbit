namespace Orbit.Domain.Users.ValueObjects;

public sealed record Username
{
    public string Value { get; }

    private Username(string value) => Value = value;

    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username is required", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length is < 3 or > 50)
            throw new ArgumentException("Username length must be 3-50 characters", nameof(value));

        return new Username(trimmed);
    }

    public override string ToString() => Value;
}

