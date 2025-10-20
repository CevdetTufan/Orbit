using Orbit.Application.Users;
using System.Collections.Concurrent;

namespace Orbit.Web.Services;

/// <summary>
/// Manages Blazor Server user sessions and circuit tracking
/// </summary>
public sealed class BlazorUserSessionManager : IUserSessionManager
{
    // Kullanýcý adý -> Circuit ID listesi mapping
    private readonly ConcurrentDictionary<string, HashSet<string>> _userCircuits = new();
    private readonly ILogger<BlazorUserSessionManager> _logger;
    
    public BlazorUserSessionManager(ILogger<BlazorUserSessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a new circuit for a user
    /// </summary>
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
/// Static notifier for circuit termination callbacks
/// </summary>
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
