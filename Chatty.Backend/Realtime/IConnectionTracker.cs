namespace Chatty.Backend.Realtime;

public interface IConnectionTracker
{
    Task AddConnectionAsync(Guid userId, string connectionId);
    Task RemoveConnectionAsync(Guid userId, string connectionId);
    Task<IReadOnlyList<string>> GetConnectionsAsync(Guid userId);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetConnectionsAsync(IEnumerable<Guid> userIds);
    Task<bool> IsOnlineAsync(Guid userId);
}