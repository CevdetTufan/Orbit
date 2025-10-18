using Orbit.Application.Auth;
using Orbit.Domain.Authorization;
using Orbit.Domain.Common;
using Orbit.Domain.Users;
using Orbit.Domain.Users.ValueObjects;

namespace Orbit.Application.Users;

public interface IUserCommands
{
    Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default);
    Task<Guid> CreateWithPasswordAsync(string username, string email, string password, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string username, string email, CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task AssignMultipleRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
}

internal sealed class UserCommands : IUserCommands
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IReadRepository<Role, Guid> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserCredentialStore _credentialStore;
    private readonly IUserUniquenessChecker _uniquenessChecker;

    public UserCommands(
        IRepository<User, Guid> userRepository,
        IReadRepository<Role, Guid> roleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IUserCredentialStore credentialStore,
        IUserUniquenessChecker uniquenessChecker)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _credentialStore = credentialStore;
        _uniquenessChecker = uniquenessChecker;
    }

    public async Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        // Check username uniqueness
        var isUsernameTaken = await _uniquenessChecker.IsUsernameTakenAsync(username, null, cancellationToken);
        if (isUsernameTaken)
        {
            throw new UsersDomainException($"Kullanýcý adý '{username}' zaten kullanýlýyor.");
        }

        var user = User.Create(username, email);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<Guid> CreateWithPasswordAsync(string username, string email, string password, CancellationToken cancellationToken = default)
    {
        var id = await CreateAsync(username, email, cancellationToken);
        var hash = _passwordHasher.Hash(password);
        await _credentialStore.SetAsync(id, hash, cancellationToken);
        return id;
    }

    public async Task UpdateAsync(Guid id, string username, string email, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check username uniqueness
            var isUsernameTaken = await _uniquenessChecker.IsUsernameTakenAsync(username, id, cancellationToken);
            if (isUsernameTaken)
            {
                throw new UsersDomainException($"Kullanýcý adý '{username}' zaten kullanýlýyor.");
            }

            var user = await _userRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            user.UpdateUsername(Username.Create(username));
            user.UpdateEmail(Email.Create(email));
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                // Check username uniqueness again in retry
                var isUsernameTaken = await _uniquenessChecker.IsUsernameTakenAsync(username, id, cancellationToken);
                if (isUsernameTaken)
                {
                    throw new UsersDomainException($"Kullanýcý adý '{username}' zaten kullanýlýyor.");
                }

                var freshUser = await _userRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                freshUser.UpdateUsername(Username.Create(username));
                freshUser.UpdateEmail(Email.Create(email));
                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    public async Task UpdatePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var hash = _passwordHasher.Hash(newPassword);
            await _credentialStore.SetAsync(id, hash, cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var hash = _passwordHasher.Hash(newPassword);
                await _credentialStore.SetAsync(id, hash, cancellationToken);
            });
        }
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            user.Activate();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshUser = await _userRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                freshUser.Activate();
                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            user.Deactivate();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshUser = await _userRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                freshUser.Deactivate();
                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
                ?? throw new InvalidOperationException("Role not found");

            user.AssignRole(role);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                var freshRole = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
                    ?? throw new InvalidOperationException("Role not found");
                freshUser.AssignRole(freshRole);
                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            user.RemoveRole(roleId);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                freshUser.RemoveRole(roleId);
                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    public async Task AssignMultipleRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            var roles = await _roleRepository.ListAsync(r => roleIds.Contains(r.Id), cancellationToken);
            
            if (roles.Count != roleIds.Count())
                throw new InvalidOperationException("One or more roles not found");

            foreach (var role in roles)
            {
                user.AssignRole(role);
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await HandleConcurrencyAndRetryAsync(async () =>
            {
                var freshUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                    ?? throw new InvalidOperationException("User not found");
                var freshRoles = await _roleRepository.ListAsync(r => roleIds.Contains(r.Id), cancellationToken);
                
                if (freshRoles.Count != roleIds.Count())
                    throw new InvalidOperationException("One or more roles not found");

                foreach (var role in freshRoles)
                {
                    freshUser.AssignRole(role);
                }

                _userRepository.Update(freshUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            });
        }
    }

    // Helper methods for concurrency handling
    private static async Task HandleConcurrencyAndRetryAsync(Func<Task> retryOperation)
    {
        try
        {
            await retryOperation();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new InvalidOperationException(
                "The operation could not be completed due to concurrent modifications. Please refresh and try again.",
                ex);
        }
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        return ex.Message.Contains("database operation was expected to affect") ||
               ex.Message.Contains("concurrency") ||
               ex.Message.Contains("cannot be tracked because another instance") ||
               ex.Message.Contains("is already being tracked") ||
               ex.GetType().Name.Contains("Concurrency") ||
               ex.GetType().Name.Contains("DbUpdateConcurrency") ||
               ex.GetType().Name.Contains("InvalidOperation");
    }
}
