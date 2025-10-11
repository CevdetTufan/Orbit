using Orbit.Domain.Common;
using Orbit.Domain.Users;

namespace Orbit.Application.Users;

public interface IUserCommands
{
    Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default);
}

internal sealed class UserCommands : IUserCommands
{
    private readonly IWriteRepository<User, Guid> _writeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserCommands(IWriteRepository<User, Guid> writeRepository, IUnitOfWork unitOfWork)
    {
        _writeRepository = writeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        var user = User.Create(username, email);
        await _writeRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}

