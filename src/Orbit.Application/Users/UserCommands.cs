using Orbit.Application.Auth;
using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users;

public interface IUserCommands
{
    Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default);
    Task<Guid> CreateWithPasswordAsync(string username, string email, string password, CancellationToken cancellationToken = default);
}

internal sealed class UserCommands : IUserCommands
{
    private readonly IWriteRepository<User, Guid> _writeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserCredentialStore _credentialStore;

    public UserCommands(IWriteRepository<User, Guid> writeRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IUserCredentialStore credentialStore)
    {
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _credentialStore = credentialStore;
    }

    public async Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        var user = User.Create(username, email);
        await _writeRepository.AddAsync(user, cancellationToken);
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
}
