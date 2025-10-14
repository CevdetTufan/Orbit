using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Orbit.Application;
using Orbit.Infrastructure;
using Orbit.Infrastructure.Persistence;
using Orbit.Infrastructure.Seeding;
using Orbit.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

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

// Persist DataProtection keys so auth session (localStorage protected payload)
// can be restored across application restarts.
var keysPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orbit", "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Orbit.Web");
// Circuit-scoped auth state (no cookies/controllers)
builder.Services.AddScoped<Orbit.Web.Security.CircuitAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<Orbit.Web.Security.CircuitAuthenticationStateProvider>());
// Request context for audit logging
builder.Services.AddScoped<Orbit.Application.Auth.IClientContext, Orbit.Web.Security.HttpRequestContext>();

// JWT Bearer authentication
// Authorization services for AuthorizeView/RouteView; cookie/JWT auth not needed for circuit state
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

// No cookie/JWT middleware needed; auth handled via CircuitAuthenticationStateProvider

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
