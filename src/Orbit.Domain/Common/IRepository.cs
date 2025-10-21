using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Orbit.Domain.Common;

public interface IRepository<TAggregate, TId>
    where TAggregate : Entity<TId>, IAggregateRoot
{
    // Get single tracked entity
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    // List and predicate operations
    Task<IReadOnlyList<TAggregate>> ListAsync(
        Expression<Func<TAggregate, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        BaseSpecification<TAggregate, TResult> specification,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TAggregate, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TAggregate, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<TResult?> FirstOrDefaultAsync<TResult>(
        BaseSpecification<TAggregate, TResult> specification,
        CancellationToken cancellationToken = default) where TResult : class;

    // Write operations
    Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default);
    void Update(TAggregate entity);
    void Remove(TAggregate entity);
}
