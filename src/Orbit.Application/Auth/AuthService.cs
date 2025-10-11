using Orbit.Application.Users.Specifications;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IReadRepository<User, Guid> _users;
    private readonly IReadRepository<Role, Guid> _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserCredentialStore _credentialStore;

    public AuthService(
        IReadRepository<User, Guid> users,
        IReadRepository<Role, Guid> roles,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUserCredentialStore credentialStore)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _credentialStore = credentialStore;
    }

    public async Task<AuthTokenDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // 1) Find user by username with roles included
        var user = await _users.FirstOrDefaultAsync(new UserByUsernameWithRolesSpec(username), cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        // 2) Verify credentials
        var cred = await _credentialStore.GetByUserIdAsync(user.Id, cancellationToken);
        if (cred is null || !_passwordHasher.Verify(password, cred.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // 3) Load role names
        var roleIds = user.Roles.Select(l => l.RoleId).Distinct().ToArray();
        var roles = roleIds.Length == 0
            ? Array.Empty<string>()
            : (await _roles.ListAsync(r => roleIds.Contains(r.Id), cancellationToken)).Select(r => r.Name).ToArray();

        // 4) Issue JWT
        var nowUtc = DateTime.UtcNow;
        var token = _tokenService.CreateToken(user.Id, user.Username.Value, user.Email.Value, roles, nowUtc, out var expires);

        return new AuthTokenDto(token, expires, user.Username.Value, user.Email.Value, roles);
    }
}

public interface IUserCredentialStore
{
    Task<UserCredential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
}

public sealed class UserCredential
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}
