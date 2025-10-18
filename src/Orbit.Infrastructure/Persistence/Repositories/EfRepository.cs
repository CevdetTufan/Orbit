using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Common;
using Orbit.Infrastructure.Persistence.Specifications;

namespace Orbit.Infrastructure.Persistence.Repositories;

internal class EfRepository<TAggregate, TId> : IRepository<TAggregate, TId>
	where TAggregate : Entity<TId>, IAggregateRoot
{
	private readonly AppDbContext _dbContext;
	private readonly DbSet<TAggregate> _set;

	public EfRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
		_set = _dbContext.Set<TAggregate>();
	}

	// IReadRepository
	public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
	{
		// FindAsync returns a tracked entity - Update operations için gerekli
		// Write operations için bu method kullanýlacak
		return await _set.FindAsync(new object?[] { id }, cancellationToken);
	}

	public async Task<IReadOnlyList<TAggregate>> ListAsync(
		Expression<Func<TAggregate, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		// ListAsync always uses AsNoTracking for read-only operations
		IQueryable<TAggregate> query = _set.AsNoTracking();
		if (predicate is not null)
		{
			query = query.Where(predicate);
		}
		return await query.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
				BaseSpecification<TAggregate, TResult> specification,
				CancellationToken cancellationToken = default)
	{
		var query = SpecificationEvaluator.GetQuery<TAggregate, TResult>(_set.AsQueryable(), specification);
		return await query.ToListAsync(cancellationToken);
	}

	public Task<bool> AnyAsync(
		Expression<Func<TAggregate, bool>> predicate,
		CancellationToken cancellationToken = default)
		=> _set.AsNoTracking().AnyAsync(predicate, cancellationToken);

	public Task<int> CountAsync(
		Expression<Func<TAggregate, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
		=> predicate is null
			? _set.AsNoTracking().CountAsync(cancellationToken)
			: _set.AsNoTracking().CountAsync(predicate, cancellationToken);

	public async Task<IReadOnlyList<TAggregate>> ListAsync(
		ISpecification<TAggregate> specification,
		CancellationToken cancellationToken = default)
	{
		// Don't use AsNoTracking for entity specifications
		// Write operations need tracked entities
		var query = SpecificationEvaluator.GetQuery(_set.AsQueryable(), specification);
		return await query.ToListAsync(cancellationToken);
	}

	public Task<int> CountAsync(
		ISpecification<TAggregate> specification,
		CancellationToken cancellationToken = default)
	{
		var query = SpecificationEvaluator.GetQuery(_set.AsQueryable(), specification);
		return query.CountAsync(cancellationToken);
	}

	public async Task<TAggregate?> FirstOrDefaultAsync(
		ISpecification<TAggregate> specification,
		CancellationToken cancellationToken = default)
	{
		// Don't use AsNoTracking - let specification decide via DisableTracking()
		var query = SpecificationEvaluator.GetQuery(_set.AsQueryable(), specification);
		return await query.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<TResult?> FirstOrDefaultAsync<TResult>(
		BaseSpecification<TAggregate, TResult> specification,
		CancellationToken cancellationToken = default) where TResult : class
	{
		var query = SpecificationEvaluator.GetQuery<TAggregate, TResult>(_set.AsQueryable(), specification);
		return await query.FirstOrDefaultAsync(cancellationToken);
	}

	// IWriteRepository
	public Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default)
		=> _set.AddAsync(entity, cancellationToken).AsTask();

	public void Update(TAggregate entity) => _set.Update(entity);

	public void Remove(TAggregate entity) => _set.Remove(entity);
}
