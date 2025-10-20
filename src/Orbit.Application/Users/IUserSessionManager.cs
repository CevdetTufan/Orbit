namespace Orbit.Application.Users;

/// <summary>
/// Manages user sessions across the application.
/// </summary>
/// <remarks>
/// <para><b>Current Implementation:</b> Blazor Server Circuit Tracking (In-Memory)</para>
/// <para><b>Suitable for:</b> Single-server deployments with &lt; 100K active users</para>
/// <para>
/// <b>?? Scalability Considerations:</b>
/// <list type="bullet">
/// <item><description>Memory usage: ~500 MB - 1 GB for 1M users</description></item>
/// <item><description>Concurrency bottleneck at ~10K concurrent operations</description></item>
/// <item><description>Does NOT work in load-balanced / multi-server environments</description></item>
/// <item><description>Risk of memory leaks from orphaned circuits</description></item>
/// </list>
/// </para>
/// <para>
/// <b>?? See:</b> <c>docs/scalability-risks-and-solutions.md</c> for detailed analysis and migration recommendations.
/// </para>
/// <para>
/// <b>?? Action Required:</b> Migrate to Redis-based implementation when reaching 100K+ users or deploying to multiple servers.
/// </para>
/// </remarks>
public interface IUserSessionManager
{
    /// <summary>
    /// Terminates all active sessions for a specific user.
    /// </summary>
    /// <param name="username">Username whose sessions should be terminated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <remarks>
    /// <para>This method is typically called when:</para>
    /// <list type="bullet">
    /// <item><description>User is deactivated by admin</description></item>
    /// <item><description>User's password is forcefully changed</description></item>
    /// <item><description>Security breach requires immediate logout</description></item>
    /// </list>
    /// <para>
    /// <b>?? Multi-Server Warning:</b> In load-balanced environments with in-memory implementation,
    /// this will only terminate sessions on the current server. For cross-server support, use Redis implementation.
    /// </para>
    /// </remarks>
    Task TerminateUserSessionsAsync(string username, CancellationToken cancellationToken = default);
}
