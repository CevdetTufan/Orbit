namespace Orbit.Domain.Common;

public interface IRepository<TAggregate, TId> : IReadRepository<TAggregate, TId>, IWriteRepository<TAggregate, TId>
    where TAggregate : Entity<TId>, IAggregateRoot;
