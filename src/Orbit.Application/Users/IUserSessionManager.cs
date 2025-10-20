namespace Orbit.Application.Users;

/// <summary>
/// Manages user sessions across the application
/// </summary>
public interface IUserSessionManager
{
    /// <summary>
    /// Terminates all active sessions for a specific user
    /// </summary>
    /// <param name="username">Username whose sessions should be terminated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TerminateUserSessionsAsync(string username, CancellationToken cancellationToken = default);
}
