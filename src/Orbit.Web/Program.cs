using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Orbit.Application;
using Orbit.Application.Users;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Infrastructure;
using Orbit.Infrastructure.Persistence;
using Orbit.Web.Components;
using Orbit.Web.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Application & Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddJwt(o => builder.Configuration.GetSection("Jwt").Bind(o));
builder.Services.AddCascadingAuthenticationState();

// JWT Bearer authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new()
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwt["SigningKey"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Apply EF Core migrations on startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Minimal API endpoints demonstrating repository usage
app.MapGet("/api/users", async (IUserQueries queries, CancellationToken ct) =>
{
    var users = await queries.GetAllAsync(ct);
    return Results.Ok(users);
}).RequireAuthorization();

app.MapPost("/api/users", async (IUserCommands users, string username, string email, CancellationToken ct) =>
{
    var id = await users.CreateAsync(username, email, ct);
    return Results.Created($"/api/users/{id}", new { Id = id });
}).RequireAuthorization();

// Simple specification-based search via Application layer
app.MapGet("/api/users/search", async (IUserQueries queries, string q, CancellationToken ct) =>
{
    var users = await queries.SearchAsync(q, ct);
    return Results.Ok(users);
}).RequireAuthorization();

// Auth endpoint
app.MapPost("/api/auth/login", async (Orbit.Application.Auth.IAuthService auth, HttpContext http, string username, string password, CancellationToken ct) =>
{
    var token = await auth.LoginAsync(username, password, ct);

    // Build cookie principal
    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, token.Username),
        new(System.Security.Claims.ClaimTypes.Email, token.Email)
    };
    claims.AddRange(token.Roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));

    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = token.ExpiresAtUtc
    });

    // Redirect to home after login
    return Results.Redirect("/");
}).DisableAntiforgery();

// Dev-only register endpoint (optional): create user with password
app.MapPost("/api/auth/register", async (IUserCommands users, RegisterRequest body, CancellationToken ct) =>
{
    var id = await users.CreateWithPasswordAsync(body.Username, body.Email, body.Password, ct);
    return Results.Created($"/api/users/{id}", new { Id = id });
}).DisableAntiforgery();

await app.RunAsync();
