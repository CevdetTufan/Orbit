using Orbit.Application.Auth;
using Orbit.Application.Users.Specifications;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Application.Account;

public interface IAccountService
{
    Task UpdateEmailAsync(string username, string newEmail, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

internal sealed class AccountService : IAccountService
{
    private readonly IReadRepository<User, Guid> _usersRead;
    private readonly IWriteRepository<User, Guid> _usersWrite;
    private readonly IUnitOfWork _uow;
    private readonly IUserCredentialStore _credentialStore;
    private readonly IPasswordHasher _passwordHasher;

    public AccountService(
        IReadRepository<User, Guid> usersRead,
        IWriteRepository<User, Guid> usersWrite,
        IUnitOfWork uow,
        IUserCredentialStore credentialStore,
        IPasswordHasher passwordHasher)
    {
        _usersRead = usersRead;
        _usersWrite = usersWrite;
        _uow = uow;
        _credentialStore = credentialStore;
        _passwordHasher = passwordHasher;
    }

    public async Task UpdateEmailAsync(string username, string newEmail, CancellationToken cancellationToken = default)
    {
        var user = await _usersRead.FirstOrDefaultAsync(new UserByUsernameWithRolesSpec(username), cancellationToken)
                   ?? throw new InvalidOperationException("User not found");

        user.UpdateEmail(Email.Create(newEmail));
        _usersWrite.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _usersRead.FirstOrDefaultAsync(new UserByUsernameWithRolesSpec(username), cancellationToken)
                   ?? throw new InvalidOperationException("User not found");

        var cred = await _credentialStore.GetByUserIdAsync(user.Id, cancellationToken)
                   ?? throw new InvalidOperationException("Credentials not found");

        if (!_passwordHasher.Verify(currentPassword, cred.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        var newHash = _passwordHasher.Hash(newPassword);
        await _credentialStore.SetAsync(user.Id, newHash, cancellationToken);
    }
}
