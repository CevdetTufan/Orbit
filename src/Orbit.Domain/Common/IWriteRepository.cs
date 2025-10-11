namespace Orbit.Domain.Common;

public interface IWriteRepository<TAggregate, TId>
    where TAggregate : Entity<TId>, IAggregateRoot
{
    Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default);
    void Update(TAggregate entity);
    void Remove(TAggregate entity);
}

