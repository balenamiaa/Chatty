namespace Chatty.Client.Realtime;

/// <summary>
///     Syncs local cache with realtime updates from SignalR
/// </summary>
public interface IRealtimeSyncService : IDisposable
{
    /// <summary>
    ///     Starts listening for realtime updates and syncing with cache
    /// </summary>
    void Start();
}
