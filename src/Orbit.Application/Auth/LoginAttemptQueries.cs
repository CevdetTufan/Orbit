using Orbit.Application.Auth.Specifications;
using Orbit.Domain.Common;
using Orbit.Domain.Security;

namespace Orbit.Application.Auth;

public interface ILoginAttemptQueries
{
    Task<int> CountAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAttempt>> GetPageAsync(string username, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

internal sealed class LoginAttemptQueries : ILoginAttemptQueries
{
    private readonly IReadRepository<LoginAttempt, Guid> _loginAttempts;

    public LoginAttemptQueries(IReadRepository<LoginAttempt, Guid> loginAttempts)
    {
        _loginAttempts = loginAttempts;
    }

    public Task<int> CountAsync(string username, CancellationToken cancellationToken = default)
    {
        return _loginAttempts.CountAsync(new LoginAttemptsByUsernameFilterSpec(username), cancellationToken);
    }

    public async Task<IReadOnlyList<LoginAttempt>> GetPageAsync(string username, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var spec = new LoginAttemptsByUsernamePagedSpec(username, pageIndex, pageSize);
        var list = await _loginAttempts.ListAsync(spec, cancellationToken);
        return list.ToList();
    }
}

