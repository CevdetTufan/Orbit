using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;
using Orbit.Application.Auth;
using Orbit.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Orbit.Web.Security;

public sealed class CircuitAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IJSRuntime _js;
    private readonly IDataProtector _protector;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BlazorUserSessionManager _sessionManager;
    private readonly NavigationManager _navigationManager;
    private const string StorageKey = "auth_session";

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private string? _currentCircuitId;
    private string? _currentUsername;

    public CircuitAuthenticationStateProvider(
        IJSRuntime js,
        IDataProtectionProvider dp,
        IHttpContextAccessor httpContextAccessor,
        BlazorUserSessionManager sessionManager,
        NavigationManager navigationManager)
    {
        _js = js;
        _protector = dp.CreateProtector("Orbit.Web.AuthSession.v1");
        _httpContextAccessor = httpContextAccessor;
        _sessionManager = sessionManager;
        _navigationManager = navigationManager;
        
        // Get circuit ID from connection ID (Blazor Server unique identifier)
        _currentCircuitId = _httpContextAccessor.HttpContext?.Connection.Id;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public async Task SignInAsync(AuthTokenDto token)
    {
        var session = new AuthSession(token.Username, token.Email, token.Roles, token.ExpiresAtUtc);
        await SaveAsync(session);
        _currentUser = BuildPrincipal(session);
        _currentUsername = token.Username;
        
        // Register circuit with session manager
        if (_currentCircuitId != null)
        {
            _sessionManager.RegisterCircuit(token.Username, _currentCircuitId);
            
            // Register termination callback
            CircuitTerminationNotifier.RegisterCallback(_currentCircuitId, () =>
            {
                _ = ForceLogoutAsync();
            });
        }
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        // Unregister from session manager
        if (_currentCircuitId != null && _currentUsername != null)
        {
            _sessionManager.UnregisterCircuit(_currentUsername, _currentCircuitId);
            CircuitTerminationNotifier.UnregisterCallback(_currentCircuitId);
        }
        
        await RemoveAsync();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        _currentUsername = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Force logout when user is deactivated
    /// </summary>
    private async Task ForceLogoutAsync()
    {
        await SignOutAsync();
        _navigationManager.NavigateTo("/login?reason=deactivated", forceLoad: true);
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
                _currentUsername = session.Username;
                
                // Register circuit with session manager on restore
                if (_currentCircuitId != null)
                {
                    _sessionManager.RegisterCircuit(session.Username, _currentCircuitId);
                    
                    // Register termination callback
                    CircuitTerminationNotifier.RegisterCallback(_currentCircuitId, () =>
                    {
                        _ = ForceLogoutAsync();
                    });
                }
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

    public void Dispose()
    {
        // Cleanup on dispose
        if (_currentCircuitId != null && _currentUsername != null)
        {
            _sessionManager.UnregisterCircuit(_currentUsername, _currentCircuitId);
            CircuitTerminationNotifier.UnregisterCallback(_currentCircuitId);
        }
    }

    public sealed record AuthSession(string Username, string Email, IReadOnlyList<string> Roles, DateTime ExpiresAtUtc);
}
