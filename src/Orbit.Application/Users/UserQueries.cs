using Orbit.Application.Common.Models;
using Orbit.Application.Users.Models;
using Orbit.Application.Users.Specifications;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users;

public interface IUserQueries
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<PagedResult<UserListItemDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchQuery = null, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
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

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var spec = new UsersPagedSpec(pageIndex, pageSize, searchQuery);
        var users = await _readRepository.ListAsync(spec, cancellationToken);
        
        // Client-side sýralama - EF Core Value Object'i translate edemediði için
        var sortedUsers = users.OrderBy(u => u.Username).ToList();
        
        var countSpec = new UsersCountSpec(searchQuery);
        var totalCount = await _readRepository.CountAsync(countSpec, cancellationToken);

        return new PagedResult<UserListItemDto>
        {
            Items = sortedUsers,
            TotalCount = totalCount
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var spec = new UserByIdWithRolesSpec(id);
        return await _readRepository.FirstOrDefaultAsync(spec, cancellationToken);
    }
}
