using Orbit.Domain.Common;

namespace Orbit.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Explicitly detect changes before saving
        // This ensures collection changes (like adding UserRole) are properly detected
        _dbContext.ChangeTracker.DetectChanges();
        
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

