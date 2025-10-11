using System.Linq.Expressions;

namespace Orbit.Domain.Common;

public interface IReadRepository<TAggregate, TId>
    where TAggregate : Entity<TId>, IAggregateRoot
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(
        Expression<Func<TAggregate, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TAggregate, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TAggregate, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // Specification support
    Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);
}
