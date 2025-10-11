using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;
using Orbit.Application.Auth;

namespace Orbit.Web.Security;

public sealed class CircuitAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly IDataProtector _protector;
    private const string StorageKey = "orbit.auth";

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CircuitAuthenticationStateProvider(IJSRuntime js, IDataProtectionProvider dp)
    {
        _js = js;
        _protector = dp.CreateProtector("Orbit.Web.AuthSession.v1");
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public async Task SignInAsync(AuthTokenDto token)
    {
        var session = new AuthSession(token.Username, token.Email, token.Roles, token.ExpiresAtUtc);
        await SaveAsync(session);
        _currentUser = BuildPrincipal(session);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await RemoveAsync();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public bool IsRestored { get; private set; }

    public async Task RestoreAsync()
    {
        if (IsRestored) return;
        try
        {
            var session = await LoadAsync();
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
        finally
        {
            IsRestored = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    private async Task<AuthSession?> LoadAsync()
    {
        try
        {
            var cipher = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(cipher)) return null;
            var json = _protector.Unprotect(cipher);
            return JsonSerializer.Deserialize<AuthSession>(json);
        }
        catch { return null; }
    }

    private async Task SaveAsync(AuthSession session)
    {
        var json = JsonSerializer.Serialize(session);
        var cipher = _protector.Protect(json);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, cipher);
    }

    private async Task RemoveAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
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
