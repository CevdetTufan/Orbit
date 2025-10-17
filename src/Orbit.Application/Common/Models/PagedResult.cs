namespace Orbit.Application.Common.Models;

public class PagedResult<T>
{
	public int TotalCount { get; init; }
	public IReadOnlyList<T> Items { get; init; } = [];
}