namespace Orbit.Application.Auth;

public sealed record AuthTokenDto(string AccessToken, DateTime ExpiresAtUtc, string Username, string Email, IReadOnlyList<string> Roles);

public interface IAuthService
{
    Task<AuthTokenDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    string CreateToken(Guid userId, string username, string email, IEnumerable<string> roles, DateTime nowUtc, out DateTime expiresAtUtc);
}

public interface IClientContext
{
    string? RemoteIp { get; }
    string? UserAgent { get; }
}
