using Microsoft.EntityFrameworkCore;
using Orbit.Application;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Infrastructure;
using Orbit.Infrastructure.Persistence;
using Orbit.Web.Components;
using Orbit.Application.Users.Specifications;

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
app.MapGet("/api/users", async (IReadRepository<User, Guid> repo, CancellationToken ct) =>
{
    var users = await repo.ListAsync(cancellationToken: ct);
    return Results.Ok(users.Select(u => new
    {
        u.Id,
        Username = u.Username.Value,
        Email = u.Email.Value,
        u.IsActive
    }));
});

app.MapPost("/api/users", async (IWriteRepository<User, Guid> repo, IUnitOfWork uow, string username, string email, CancellationToken ct) =>
{
    var user = User.Create(username, email);
    await repo.AddAsync(user, ct);
    await uow.SaveChangesAsync(ct);
    return Results.Created($"/api/users/{user.Id}", new { user.Id });
});

// Simple specification-based search
app.MapGet("/api/users/search", async (IReadRepository<User, Guid> repo, string q, CancellationToken ct) =>
{
    var spec = new UsersByQuerySpec(q);
    var users = await repo.ListAsync(spec, ct);
    return Results.Ok(users.Select(u => new { u.Id, Username = u.Username.Value, Email = u.Email.Value }));
});

await app.RunAsync();
