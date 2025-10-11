using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Orbit.Application;
using Orbit.Infrastructure;
using Orbit.Infrastructure.Persistence;
using Orbit.Infrastructure.Seeding;
using Orbit.Web.Components;

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
builder.Services.AddHttpContextAccessor();
// Circuit-scoped auth state (no cookies/controllers)
builder.Services.AddScoped<Orbit.Web.Security.CircuitAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<Orbit.Web.Security.CircuitAuthenticationStateProvider>());
// Request context for audit logging
builder.Services.AddScoped<Orbit.Application.Auth.IClientContext, Orbit.Web.Security.HttpRequestContext>();

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

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();

	// Seed sample data in Development
	if (app.Environment.IsDevelopment())
	{
		var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
		await seeder.SeedAsync();
	}
}

await app.RunAsync();
