using System.Collections.Concurrent;

namespace Chatty.Backend.Realtime;

public sealed class ConnectionTracker(ILogger<ConnectionTracker> logger) : IConnectionTracker
{
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public Task AddConnectionAsync(Guid userId, string connectionId)
    {
        _connections.AddOrUpdate(
            userId,
            _ => [connectionId],
            (_, connections) =>
            {
                connections.Add(connectionId);
                return connections;
            });

        logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, connectionId);
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _connections.TryRemove(userId, out _);
            }
        }

        logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, connectionId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetConnectionsAsync(Guid userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            return Task.FromResult<IReadOnlyList<string>>(connections.ToList());
        }

        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetConnectionsAsync(IEnumerable<Guid> userIds)
    {
        var result = userIds
            .ToDictionary(
                id => id,
                id => _connections.TryGetValue(id, out var connections)
                    ? (IReadOnlyList<string>)connections.ToList()
                    : Array.Empty<string>());

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(result);
    }

    public Task<bool> IsOnlineAsync(Guid userId)
    {
        return Task.FromResult(_connections.TryGetValue(userId, out var connections) && connections.Count > 0);
    }
}
