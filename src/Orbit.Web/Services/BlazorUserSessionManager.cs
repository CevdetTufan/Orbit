using Orbit.Application.Users;
using System.Collections.Concurrent;

namespace Orbit.Web.Services;

/// <summary>
/// Manages Blazor Server user sessions and circuit tracking using in-memory storage.
/// </summary>
/// <remarks>
/// <para><b>?? SCALABILITY WARNING:</b></para>
/// <list type="bullet">
/// <item><description><b>Suitable for:</b> Single-server deployments with &lt; 100K active users</description></item>
/// <item><description><b>Memory usage:</b> ~1 GB for 1M users (in-memory dictionary)</description></item>
/// <item><description><b>Multi-server:</b> Does NOT work in load-balanced environments</description></item>
/// <item><description><b>Memory leak risk:</b> Orphaned circuits can accumulate if not properly cleaned up</description></item>
/// </list>
/// <para>
/// <b>?? Migration Required:</b> When reaching 100K+ users or deploying to multiple servers,
/// migrate to Redis-based implementation. See <c>docs/scalability-risks-and-solutions.md</c>
/// </para>
/// <para>
/// <b>Alternative Implementations:</b>
/// <list type="bullet">
/// <item><description><c>RedisUserSessionManager</c> - For distributed environments</description></item>
/// <item><description><c>SignalRUserSessionManager</c> - With SignalR Backplane for real-time notifications</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BlazorUserSessionManager : IUserSessionManager
{
    // Kullanýcý adý -> Circuit ID listesi mapping
    // ?? IN-MEMORY: Tek sunucu sýnýrlý, restart'ta kaybolur
    private readonly ConcurrentDictionary<string, HashSet<string>> _userCircuits = new();
    private readonly ILogger<BlazorUserSessionManager> _logger;
    
    public BlazorUserSessionManager(ILogger<BlazorUserSessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a new circuit for a user
    /// </summary>
    /// <param name="username">Username to register circuit for</param>
    /// <param name="circuitId">Unique circuit identifier (Blazor Server connection ID)</param>
    /// <remarks>
    /// <para>Called when user logs in or establishes new Blazor Server connection.</para>
    /// <para><b>?? Memory Consideration:</b> Each registration consumes ~200 bytes of memory.</para>
    /// </remarks>
    public void RegisterCircuit(string username, string circuitId)
    {
        _userCircuits.AddOrUpdate(
            username,
            _ => new HashSet<string> { circuitId },
            (_, circuits) =>
            {
                lock (circuits)
                {
                    circuits.Add(circuitId);
                }
                return circuits;
            });
        
        _logger.LogDebug("Circuit {CircuitId} registered for user {Username}", circuitId, username);
    }

    /// <summary>
    /// Unregister a circuit for a user
    /// </summary>
    /// <param name="username">Username to unregister circuit from</param>
    /// <param name="circuitId">Circuit identifier to remove</param>
    /// <remarks>
    /// <para>Called when user logs out or circuit disconnects.</para>
    /// <para><b>?? Important:</b> If this method is not called (e.g., browser crash), 
    /// circuit will remain in memory until server restart, causing memory leak.</para>
    /// </remarks>
    public void UnregisterCircuit(string username, string circuitId)
    {
        if (_userCircuits.TryGetValue(username, out var circuits))
        {
            lock (circuits)
            {
                circuits.Remove(circuitId);
                if (circuits.Count == 0)
                {
                    _userCircuits.TryRemove(username, out _);
                }
            }
        }
        
        _logger.LogDebug("Circuit {CircuitId} unregistered for user {Username}", circuitId, username);
    }

    /// <summary>
    /// Terminate all sessions for a specific user
    /// </summary>
    /// <param name="username">Username whose sessions should be terminated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <remarks>
    /// <para>Called when user is deactivated or requires forced logout.</para>
    /// <para>
    /// <b>?? Multi-Server Limitation:</b> Only terminates sessions on THIS server.
    /// In load-balanced environments, user may still have active sessions on other servers.
    /// </para>
    /// </remarks>
    public Task TerminateUserSessionsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_userCircuits.TryRemove(username, out var circuits))
        {
            _logger.LogInformation(
                "Terminating {Count} active circuit(s) for user {Username}",
                circuits.Count,
                username);

            // Notify all circuits to terminate
            foreach (var circuitId in circuits)
            {
                CircuitTerminationNotifier.NotifyTermination(circuitId);
            }
        }
        else
        {
            _logger.LogDebug("No active circuits found for user {Username}", username);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Get active circuit count for a user (for monitoring/debugging)
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <returns>Number of active circuits for the user</returns>
    public int GetActiveCircuitCount(string username)
    {
        if (_userCircuits.TryGetValue(username, out var circuits))
        {
            lock (circuits)
            {
                return circuits.Count;
            }
        }
        return 0;
    }
}

/// <summary>
/// Static notifier for circuit termination callbacks.
/// </summary>
/// <remarks>
/// <para>Uses static dictionary to maintain callbacks across service instances.</para>
/// <para><b>Pattern:</b> Observer pattern for circuit-to-logout mapping</para>
/// </remarks>
public static class CircuitTerminationNotifier
{
    private static readonly ConcurrentDictionary<string, Action> _terminationCallbacks = new();

    public static void RegisterCallback(string circuitId, Action callback)
    {
        _terminationCallbacks[circuitId] = callback;
    }

    public static void UnregisterCallback(string circuitId)
    {
        _terminationCallbacks.TryRemove(circuitId, out _);
    }

    public static void NotifyTermination(string circuitId)
    {
        if (_terminationCallbacks.TryGetValue(circuitId, out var callback))
        {
            try
            {
                callback?.Invoke();
            }
            catch
            {
                // Suppress exceptions from callbacks
            }
        }
    }
}
