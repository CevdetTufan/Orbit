using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Authorization;
using Orbit.Domain.Users;
using Orbit.Domain.Security;

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
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}
