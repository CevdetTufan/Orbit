using Orbit.Domain.Common;

namespace Orbit.Application.Common;

/// <summary>
/// Handler interface for domain events
/// </summary>
/// <typeparam name="TEvent">Type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
