using Orbit.Domain.Common;

namespace Orbit.Application.Common;

/// <summary>
/// Dispatches domain events to their handlers
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
