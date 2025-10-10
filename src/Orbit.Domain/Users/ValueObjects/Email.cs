namespace Orbit.Domain.Users.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email is required", nameof(value));

        // very lightweight guard; full RFC compliance is out of scope for domain
        if (!value.Contains('@') || value.Length < 5)
            throw new ArgumentException("Email is invalid", nameof(value));

        return new Email(value.Trim());
    }

    public override string ToString() => Value;
}

