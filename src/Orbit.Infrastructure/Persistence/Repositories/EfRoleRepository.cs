using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Authorization;
using Orbit.Infrastructure.Persistence;

namespace Orbit.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IRoleRepository.
/// Provides domain-specific operations while leveraging the generic repository base.
/// </summary>
internal sealed class EfRoleRepository : EfRepository<Role, Guid>, IRoleRepository
{
    private readonly AppDbContext _context;

    public EfRoleRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        // Check if entity is already tracked locally
        var localEntity = _context.Set<Role>().Local.FirstOrDefault(r => r.Id == roleId);
        if (localEntity != null)
        {
            // Ensure permissions are loaded
            await _context.Entry(localEntity).Collection(r => r.Permissions).LoadAsync(cancellationToken);
            return localEntity;
        }

        // Query database with includes - returns tracked entity
        return await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .AnyAsync(r => r.Name == name, cancellationToken);
    }
}