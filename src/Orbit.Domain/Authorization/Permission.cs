using Orbit.Domain.Common;

namespace Orbit.Domain.Authorization;

public sealed class Permission : Entity<Guid>, IAggregateRoot
{
    public string Code { get; private set; } = null!; 
    public string? Description { get; private set; }

    private Permission() { }

    private Permission(Guid id, string code, string? description) : base(id)
    {
        Code = ValidateCode(code);
        Description = description?.Trim();
    }

    public static Permission Create(string code, string? description = null)
        => new(Guid.NewGuid(), code, description);

    public void Rename(string code) => Code = ValidateCode(code);
    public void UpdateDescription(string? description) => Description = description?.Trim();

    private static string ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code is required", nameof(code));
        var trimmed = code.Trim();
        if (trimmed.Length > 200)
            throw new ArgumentException("Permission code too long", nameof(code));
        return trimmed;
    }
}
