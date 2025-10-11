using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Orbit.Application.Auth;

namespace Orbit.Web.Security;

public sealed class CircuitAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _storage;
    private const string StorageKey = "auth_session";

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CircuitAuthenticationStateProvider(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser.Identity?.IsAuthenticated == true)
            return new AuthenticationState(_currentUser);

        try
        {
            var result = await _storage.GetAsync<AuthSession>(StorageKey);
            var session = result.Success ? result.Value : null;
            if (session != null && session.ExpiresAtUtc > DateTime.UtcNow)
            {
                _currentUser = BuildPrincipal(session);
            }
            else
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
        }
        catch
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task SignInAsync(AuthTokenDto token)
    {
        var session = new AuthSession(token.Username, token.Email, token.Roles, token.ExpiresAtUtc);
        await _storage.SetAsync(StorageKey, session);
        _currentUser = BuildPrincipal(session);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await _storage.DeleteAsync(StorageKey);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal BuildPrincipal(AuthSession s)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, s.Username),
            new(ClaimTypes.Email, s.Email)
        };
        if (s.Roles != null)
            claims.AddRange(s.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "CircuitAuth");
        return new ClaimsPrincipal(identity);
    }

    public sealed record AuthSession(string Username, string Email, IReadOnlyList<string> Roles, DateTime ExpiresAtUtc);
}
