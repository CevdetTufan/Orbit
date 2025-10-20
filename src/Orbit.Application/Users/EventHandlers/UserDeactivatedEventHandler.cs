using Microsoft.Extensions.Logging;
using Orbit.Application.Common;
using Orbit.Domain.Users.Events;

namespace Orbit.Application.Users.EventHandlers;

/// <summary>
/// Handles UserDeactivatedEvent by terminating all active user sessions
/// </summary>
internal sealed class UserDeactivatedEventHandler : IDomainEventHandler<UserDeactivatedEvent>
{
    private readonly IUserSessionManager _sessionManager;
    private readonly ILogger<UserDeactivatedEventHandler> _logger;

    public UserDeactivatedEventHandler(
        IUserSessionManager sessionManager,
        ILogger<UserDeactivatedEventHandler> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task HandleAsync(UserDeactivatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User deactivated: {Username} (ID: {UserId}). Terminating active sessions...",
            domainEvent.Username,
            domainEvent.UserId);

        try
        {
            await _sessionManager.TerminateUserSessionsAsync(domainEvent.Username, cancellationToken);
            
            _logger.LogInformation(
                "Successfully terminated all sessions for user {Username}",
                domainEvent.Username);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the entire operation
            // Session termination is a side-effect and shouldn't rollback the deactivation
            _logger.LogError(ex, 
                "Failed to terminate sessions for user {Username}. Sessions may remain active until next validation.",
                domainEvent.Username);
        }
    }
}
