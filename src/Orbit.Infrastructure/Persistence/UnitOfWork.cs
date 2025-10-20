using Microsoft.EntityFrameworkCore;
using Orbit.Application.Common;
using Orbit.Domain.Common;

namespace Orbit.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UnitOfWork(AppDbContext dbContext, IDomainEventDispatcher eventDispatcher)
    {
        _dbContext = dbContext;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Explicitly detect changes before saving
        // This ensures collection changes (like adding UserRole) are properly detected
        _dbContext.ChangeTracker.DetectChanges();
        
        // Collect domain events from tracked entities before saving
        var domainEntities = _dbContext.ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        // Save changes to database first
        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        // After successful save, dispatch domain events
        foreach (var domainEvent in domainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        // Clear events after dispatching
        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        return result;
    }
}

