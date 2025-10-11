using Orbit.Application.Auth;

namespace Orbit.Infrastructure.Persistence.Entities;

internal sealed class UserCredentialEntity
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}

internal sealed class UserCredentialStore : IUserCredentialStore
{
    private readonly AppDbContext _dbContext;

    public UserCredentialStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserCredential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<UserCredentialEntity>().FindAsync(new object?[] { userId }, cancellationToken);
        if (entity is null) return null;
        return new UserCredential
        {
            UserId = entity.UserId,
            PasswordHash = entity.PasswordHash
        };
    }

    public async Task SetAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var set = _dbContext.Set<UserCredentialEntity>();
        var existing = await set.FindAsync(new object?[] { userId }, cancellationToken);
        if (existing is null)
        {
            await set.AddAsync(new UserCredentialEntity { UserId = userId, PasswordHash = passwordHash }, cancellationToken);
        }
        else
        {
            existing.PasswordHash = passwordHash;
            _dbContext.Update(existing);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
