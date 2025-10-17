using Orbit.Application.Auth.Models;
using Orbit.Domain.Common;
using Orbit.Domain.Security;

namespace Orbit.Application.Auth.Specifications;

public sealed class LoginAttemptsByUsernamePagedDtoSpec : BaseSpecification<LoginAttempt, LoginAttemptListItemDto>
{
	public LoginAttemptsByUsernamePagedDtoSpec(string username, int pageIndex, int pageSize)
		: base(a => a.Username == username)
	{
		if (pageIndex < 0) pageIndex = 0;
		if (pageSize <= 0) pageSize = 10;

		ApplyOrderByDescending(a => a.AttemptedAtUtc);
		ApplyPaging(pageIndex * pageSize, pageSize);

		ApplySelector(a => new LoginAttemptListItemDto
		{
			Id = a.Id,
			Username = a.Username,
			AttemptedAtUtc = a.AttemptedAtUtc,
			IsSuccessful = a.IsSuccessful,
			RemoteIp = a.RemoteIp,
			UserAgent = a.UserAgent
		});
	}
}
