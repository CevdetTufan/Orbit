using Bogus;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Infrastructure.Persistence;
using Orbit.Infrastructure.Persistence.Entities;

namespace Orbit.Infrastructure.Seeding;

internal sealed class UserDataSeeder : IDataSeeder
{
    private readonly IRepository<User, Guid> _users;
    private readonly IRepository<Role, Guid> _roles;
    private readonly IRepository<Permission, Guid> _permissions;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    private readonly Orbit.Application.Auth.IPasswordHasher _hasher;

    public UserDataSeeder(
        IRepository<User, Guid> users,
        IRepository<Role, Guid> roles,
        IRepository<Permission, Guid> permissions,
        IUnitOfWork uow,
        AppDbContext db,
        Orbit.Application.Auth.IPasswordHasher hasher)
    {
        _users = users;
        _roles = roles;
        _permissions = permissions;
        _uow = uow;
        _db = db;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Run only once: if anchor role or permission exists, skip.
        var adminRoleExists = await _roles.AnyAsync(r => r.Name == "Admin", cancellationToken);
        var readPermExists = await _permissions.AnyAsync(p => p.Code == "users.read", cancellationToken);
        if (adminRoleExists || readPermExists)
            return;

        await using var trx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1) Permissions
            var permissionCodes = new[]
            {
                "users.read", "users.write",
                "roles.read", "roles.write",
                "permissions.read", "permissions.write"
            };

            var permissionsByCode = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in permissionCodes)
            {
                var perm = Permission.Create(code, $"Permission for {code}");
                await _permissions.AddAsync(perm, cancellationToken);
                permissionsByCode[code] = perm;
            }

            // 2) Roles
            var admin = Role.Create("Admin", "System administrator");
            var manager = Role.Create("Manager", "Management role");
            var user = Role.Create("User", "Standard user role");

            foreach (var p in permissionsByCode.Values)
                admin.Grant(p);

            manager.Grant(permissionsByCode["users.read"]);
            manager.Grant(permissionsByCode["roles.read"]);
            manager.Grant(permissionsByCode["permissions.read"]);
            manager.Grant(permissionsByCode["users.write"]);

            user.Grant(permissionsByCode["users.read"]);

            await _roles.AddAsync(admin, cancellationToken);
            await _roles.AddAsync(manager, cancellationToken);
            await _roles.AddAsync(user, cancellationToken);

            // 3) Users
            var adminUser = User.Create("admin", "admin@example.com");
            adminUser.AssignRole(admin);
            await _users.AddAsync(adminUser, cancellationToken);

            var faker = new Faker("tr");
            for (int i = 0; i < 20; i++)
            {
                var username = faker.Internet.UserName().ToLowerInvariant();
                var email = faker.Internet.Email();
                var u = User.Create(username, email);

                var roll = faker.Random.Double();
                if (roll < 0.05)
                    u.AssignRole(admin);
                else if (roll < 0.30)
                    u.AssignRole(manager);
                else
                    u.AssignRole(user);

                await _users.AddAsync(u, cancellationToken);
            }

            // 4) Credentials (batched, no intermediate SaveChanges)
            var adminHash = _hasher.Hash("Admin@123!");
            var defaultHash = _hasher.Hash("Password123!");

            var credentialsSet = _db.Set<UserCredentialEntity>();

            // Include admin created above
            credentialsSet.Add(new UserCredentialEntity
            {
                UserId = adminUser.Id,
                PasswordHash = adminHash
            });

            // Fetch all users pending insert from change tracker too; we know their Ids
            var allUsers = await _users.ListAsync(cancellationToken: cancellationToken);
            foreach (var u in allUsers.Where(x => x.Id != adminUser.Id))
            {
                credentialsSet.Add(new UserCredentialEntity
                {
                    UserId = u.Id,
                    PasswordHash = defaultHash
                });
            }

            // Persist once
            await _uow.SaveChangesAsync(cancellationToken);
            await trx.CommitAsync(cancellationToken);
        }
        catch
        {
            await trx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

