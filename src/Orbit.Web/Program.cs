using Microsoft.EntityFrameworkCore;
using Orbit.Application;
using Orbit.Application.Users;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Infrastructure;
using Orbit.Infrastructure.Persistence;
using Orbit.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Application & Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure database created for demo purposes
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Minimal API endpoints demonstrating repository usage
app.MapGet("/api/users", async (IUserQueries queries, CancellationToken ct) =>
{
    var users = await queries.GetAllAsync(ct);
    return Results.Ok(users);
});

app.MapPost("/api/users", async (IUserCommands users, string username, string email, CancellationToken ct) =>
{
    var id = await users.CreateAsync(username, email, ct);
    return Results.Created($"/api/users/{id}", new { Id = id });
});

// Simple specification-based search via Application layer
app.MapGet("/api/users/search", async (IUserQueries queries, string q, CancellationToken ct) =>
{
    var users = await queries.SearchAsync(q, ct);
    return Results.Ok(users);
});

await app.RunAsync();
