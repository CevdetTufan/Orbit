using Orbit.Application.Users.Specifications;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users;

public interface IUserQueries
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

internal sealed class UserQueries : IUserQueries
{
    private readonly IReadRepository<User, Guid> _readRepository;

    public UserQueries(IReadRepository<User, Guid> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _readRepository.ListAsync(cancellationToken: cancellationToken);
        return users
            .Select(u => new UserDto(u.Id, u.Username.Value, u.Email.Value, u.IsActive))
            .ToList();
    }

    public async Task<IReadOnlyList<UserDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var spec = new UsersByQuerySpec(query);
        var users = await _readRepository.ListAsync(spec, cancellationToken);
        return users
            .Select(u => new UserDto(u.Id, u.Username.Value, u.Email.Value, u.IsActive))
            .ToList();
    }
}

public sealed record UserDto(Guid Id, string Username, string Email, bool IsActive);
