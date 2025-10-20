using Orbit.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Orbit.Application.Common;

/// <summary>
/// Default implementation of domain event dispatcher
/// </summary>
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        
        _logger.LogDebug("Dispatching domain event {EventType}", eventType.Name);
        
        // Get all handlers for this event type
        var handlers = _serviceProvider.GetServices(handlerType);
        
        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            
            try
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (method != null)
                {
                    var result = method.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (result is Task task)
                    {
                        await task;
                        _logger.LogDebug("Successfully handled {EventType} with {HandlerType}", 
                            eventType.Name, handler.GetType().Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType} with {HandlerType}", 
                    eventType.Name, handler.GetType().Name);
                // Continue with other handlers even if one fails
            }
        }
    }
}
