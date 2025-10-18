using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Authorization;
using Orbit.Infrastructure.Persistence;

namespace Orbit.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IPermissionRepository.
/// Provides permission-specific query operations.
/// </summary>
internal sealed class EfPermissionRepository : EfRepository<Permission, Guid>, IPermissionRepository
{
    private readonly AppDbContext _context;

    public EfPermissionRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AsNoTracking()
            .AnyAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var ids = permissionIds.ToList();
        var permissions = await _context.Permissions
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return permissions;
    }
}