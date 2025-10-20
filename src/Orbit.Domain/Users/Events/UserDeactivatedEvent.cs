using Orbit.Domain.Common;

namespace Orbit.Domain.Users.Events;

/// <summary>
/// Domain event raised when a user is deactivated
/// </summary>
public sealed record UserDeactivatedEvent(Guid UserId, string Username) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
