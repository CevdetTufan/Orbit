using Microsoft.AspNetCore.Http;
using Orbit.Application.Auth;

namespace Orbit.Web.Security;

public sealed class HttpRequestContext : IClientContext
{
    private readonly IHttpContextAccessor _http;

    public HttpRequestContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public string? RemoteIp
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return null;
            // Try common proxy header first
            if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var values))
            {
                var first = values.ToString().Split(',').FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first)) return first.Trim();
            }
            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent => _http.HttpContext?.Request.Headers["User-Agent"].ToString();
}

