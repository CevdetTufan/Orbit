using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Infrastructure.Domain.Users;

internal sealed class UserUniquenessChecker : IUserUniquenessChecker
{
    private readonly IReadRepository<User, Guid> _userRepository;

    public UserUniquenessChecker(IReadRepository<User, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> IsUsernameTakenAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var usernameValue = Username.Create(username);
        
        var users = await _userRepository.ListAsync(
            u => u.Username == usernameValue && (!excludeUserId.HasValue || u.Id != excludeUserId.Value),
            cancellationToken);
        
        return users.Any();
    }

    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var emailValue = Email.Create(email);
        
        var users = await _userRepository.ListAsync(
            u => u.Email == emailValue && (!excludeUserId.HasValue || u.Id != excludeUserId.Value),
            cancellationToken);
        
        return users.Any();
    }
}
