using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Common;

namespace Orbit.Infrastructure.Persistence.Specifications;

internal static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> spec)
        where T : class
    {
        IQueryable<T> query = inputQuery;

		if (spec.Criteria is not null)
        {
            query = query.Where(spec.Criteria);
        }

        if (spec.OrderBy is not null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending is not null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        if (spec.IsPagingEnabled)
        {
            if (spec.Skip is not null) query = query.Skip(spec.Skip.Value);
            if (spec.Take is not null) query = query.Take(spec.Take.Value);
        }

        foreach (var include in spec.Includes)
        {
            query = query.Include(include);
        }

		if (spec.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

		return query;
    }

	public static IQueryable<TResult> GetQuery<T, TResult>(IQueryable<T> inputQuery, BaseSpecification<T, TResult> spec)
	 where T : class
	{
		var entityQuery = GetQuery(inputQuery, (ISpecification<T>)spec);

		if (spec.Selector is not null)
		{
			return entityQuery.Select(spec.Selector);
		}

		throw new InvalidOperationException("Projection specification must provide a Selector expression.");
	}
}

