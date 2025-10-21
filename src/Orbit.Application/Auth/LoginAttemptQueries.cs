using Orbit.Application.Auth.Models;
using Orbit.Application.Auth.Specifications;
using Orbit.Application.Common.Models;
using Orbit.Domain.Common;
using Orbit.Domain.Security;

namespace Orbit.Application.Auth;

public interface ILoginAttemptQueries
{
	Task<int> CountAsync(string username, CancellationToken cancellationToken = default);
	Task<PagedResult<LoginAttemptListItemDto>> GetPagedAsync(string username, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

internal sealed class LoginAttemptQueries : ILoginAttemptQueries
{
	private readonly IRepository<LoginAttempt, Guid> _loginAttempts;

	public LoginAttemptQueries(IRepository<LoginAttempt, Guid> loginAttempts)
	{
		_loginAttempts = loginAttempts;
	}

	public Task<int> CountAsync(string username, CancellationToken cancellationToken = default)
	{
		return _loginAttempts.CountAsync(new LoginAttemptsByUsernameFilterSpec(username), cancellationToken);
	}

	public async Task<PagedResult<LoginAttemptListItemDto>> GetPagedAsync(string username, int pageIndex, int pageSize, CancellationToken cancellationToken = default)

	{
		if (string.IsNullOrWhiteSpace(username))
		{
			return new PagedResult<LoginAttemptListItemDto>
			{
				TotalCount = 0,
				Items = []
			};
		}

		if (pageIndex < 0) pageIndex = 0;
		if (pageSize <= 0) pageSize = 10;

		// Filter spec for count (entity-level)
		var filterSpec = new LoginAttemptsByUsernameFilterSpec(username);

		// DTO-spec for paged items (projection)
		var dtoSpec = new LoginAttemptsByUsernamePagedDtoSpec(username, pageIndex, pageSize);

		var countTask = _loginAttempts.CountAsync(filterSpec, cancellationToken);
		var listTask = _loginAttempts.ListAsync(dtoSpec, cancellationToken);

		await Task.WhenAll(countTask, listTask);

		return new PagedResult<LoginAttemptListItemDto>
		{
			TotalCount = await countTask,
			Items = await listTask
		};
	}
}

