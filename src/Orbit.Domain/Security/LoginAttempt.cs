using Orbit.Domain.Common;

namespace Orbit.Domain.Security;

public sealed class LoginAttempt : Entity<Guid>, IAggregateRoot
{
    public string Username { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string? RemoteIp { get; private set; }
    public string? UserAgent { get; private set; }

    private LoginAttempt() { }

    private LoginAttempt(Guid id, string username, Guid? userId, DateTime attemptedAtUtc, bool isSuccessful, string? remoteIp, string? userAgent)
        : base(id)
    {
        Username = username;
        UserId = userId;
        AttemptedAtUtc = attemptedAtUtc;
        IsSuccessful = isSuccessful;
        RemoteIp = remoteIp;
        UserAgent = userAgent;
    }

    public static LoginAttempt Create(string username, Guid? userId, DateTime attemptedAtUtc, bool isSuccessful, string? remoteIp, string? userAgent)
        => new(Guid.NewGuid(), username, userId, attemptedAtUtc, isSuccessful, remoteIp, userAgent);
}

