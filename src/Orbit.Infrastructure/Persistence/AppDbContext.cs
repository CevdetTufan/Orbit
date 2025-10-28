using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Authorization;
using Orbit.Domain.Users;
using Orbit.Domain.Security;
using Orbit.Domain.Navigation;

namespace Orbit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<Menu> Menus => Set<Menu>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}
