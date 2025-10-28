using Orbit.Domain.Common;
using Orbit.Domain.Navigation;
using Orbit.Infrastructure.Persistence;

namespace Orbit.Infrastructure.Seeding;

internal sealed class MenuDataSeeder : IDataSeeder
{
    private readonly IRepository<Menu, Guid> _menus;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public MenuDataSeeder(
        IRepository<Menu, Guid> menus,
        IUnitOfWork uow,
        AppDbContext db)
    {
        _menus = menus;
        _uow = uow;
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // If any of the anchor menus exist, skip seeding to keep idempotent
        var homeExists = await _menus.AnyAsync(m => m.Title == "Anasayfa" || m.Url == "/", cancellationToken);
        var usersExists = await _menus.AnyAsync(m => m.Title == "Kullanýcý Yönetimi" || m.Url == "/kullanici-yonetimi", cancellationToken);
        if (homeExists || usersExists)
            return;

        await using var trx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create menus with null permission
            var home = Menu.Create("Anasayfa", null, url: "/", description: "Anasayfa", parent: null, order: 1, visible: true, icon: "home");
            var users = Menu.Create("Kullanýcý Yönetimi", null, url: "/kullanici-yonetimi", description: "Kullanýcý Yönetimi", parent: null, order: 2, visible: true, icon: "users");

            await _menus.AddAsync(home, cancellationToken);
            await _menus.AddAsync(users, cancellationToken);

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
