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
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IUnitOfWork _uow;
    private readonly IUserCredentialStore _credentialStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserUniquenessChecker _uniquenessChecker;

    public AccountService(
        IRepository<User, Guid> userRepository,
        IUnitOfWork uow,
        IUserCredentialStore credentialStore,
        IPasswordHasher passwordHasher,
        IUserUniquenessChecker uniquenessChecker)
    {
        _userRepository = userRepository;
        _uow = uow;
        _credentialStore = credentialStore;
        _passwordHasher = passwordHasher;
        _uniquenessChecker = uniquenessChecker;
    }

    public async Task UpdateEmailAsync(string username, string newEmail, CancellationToken cancellationToken = default)
    {
        // Get user ID first without tracking
        var users = await _userRepository.ListAsync(
            u => u.Username == Username.Create(username), 
            cancellationToken);
        
        var user = users.FirstOrDefault() ?? throw new InvalidOperationException("User not found");
        var userId = user.Id;

        // Check email uniqueness (exclude current user)
        var isEmailTaken = await _uniquenessChecker.IsEmailTakenAsync(newEmail, userId, cancellationToken);
        if (isEmailTaken)
        {
            throw new UsersDomainException($"E-posta '{newEmail}' zaten kullanýlýyor.");
        }

        // Now get the tracked entity using GetByIdAsync
        // GetByIdAsync will handle detaching any existing tracked instance
        var trackedUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                          ?? throw new InvalidOperationException("User not found");

        trackedUser.UpdateEmail(Email.Create(newEmail));
        _userRepository.Update(trackedUser);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        // Get user ID first without tracking
        var users = await _userRepository.ListAsync(
            u => u.Username == Username.Create(username), 
            cancellationToken);
        
        var user = users.FirstOrDefault() ?? throw new InvalidOperationException("User not found");
        var userId = user.Id;

        var cred = await _credentialStore.GetByUserIdAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("Credentials not found");

        if (!_passwordHasher.Verify(currentPassword, cred.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        var newHash = _passwordHasher.Hash(newPassword);
        await _credentialStore.SetAsync(userId, newHash, cancellationToken);
    }
}
