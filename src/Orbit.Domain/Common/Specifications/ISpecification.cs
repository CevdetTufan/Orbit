using System.Linq.Expressions;

namespace Orbit.Domain.Common;

public interface ISpecification<T>
{
	Expression<Func<T, bool>>? Criteria { get; }

    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }

    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    int? Take { get; }
    int? Skip { get; }
    bool IsPagingEnabled { get; }

    bool AsNoTracking { get; }
}

public interface ISpecification<T, TResult> : ISpecification<T>
{
	Expression<Func<T, TResult>>? Selector { get; }
}

public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria;
    }

	public Expression<Func<T, bool>>? Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

	    
	protected void AddInclude(Expression<Func<T, object>> includeExpression)
        => Includes.Add(includeExpression);

    protected void AddInclude(string includeString)
        => IncludeStrings.Add(includeString);

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        => OrderBy = orderByExpression;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        => OrderByDescending = orderByDescExpression;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Enables change tracking for this query.
    /// By default, specifications use AsNoTracking for read-only operations.
    /// Call this method when you need to update entities.
    /// </summary>
    protected void EnableTracking() => AsNoTracking = false;
    
    /// <summary>
    /// Disables change tracking for this query (enables AsNoTracking).
    /// This is the default behavior and is useful for read-only operations.
    /// </summary>
    protected void DisableTracking() => AsNoTracking = true;
}

public abstract class BaseSpecification<T, TResult> : BaseSpecification<T>, ISpecification<T, TResult>
{
	protected BaseSpecification(Expression<Func<T, bool>>? criteria = null) : base(criteria) { }

	public Expression<Func<T, TResult>>? Selector { get; protected set; }

	protected void ApplySelector(Expression<Func<T, TResult>> selector)
		=> Selector = selector;
}
