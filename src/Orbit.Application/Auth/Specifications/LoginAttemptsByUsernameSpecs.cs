using Orbit.Domain.Common;
using Orbit.Domain.Security;

namespace Orbit.Application.Auth.Specifications;

public sealed class LoginAttemptsByUsernameFilterSpec : BaseSpecification<LoginAttempt>
{
    public LoginAttemptsByUsernameFilterSpec(string username)
        : base(a => a.Username == username)
    {
    }
}

public sealed class LoginAttemptsByUsernamePagedSpec : BaseSpecification<LoginAttempt>
{
    public LoginAttemptsByUsernamePagedSpec(string username, int pageIndex, int pageSize)
        : base(a => a.Username == username)
    {
        ApplyOrderByDescending(a => a.AttemptedAtUtc);
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;
        var skip = pageIndex * pageSize;
        ApplyPaging(skip, pageSize);
    }
}

