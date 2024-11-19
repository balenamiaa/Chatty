namespace Chatty.Client.State;

/// <summary>
///     Manages client-side state and synchronization
/// </summary>
public interface IStateManager
{
    /// <summary>
    ///     Get the current state
    /// </summary>
    Task<T?> GetStateAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Set the state
    /// </summary>
    Task SetStateAsync<T>(string key, T value, CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Update the state using a transform function
    /// </summary>
    Task UpdateStateAsync<T>(string key, Func<T?, T> transform, CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Remove state
    /// </summary>
    Task RemoveStateAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     Clear all state
    /// </summary>
    Task ClearStateAsync(CancellationToken ct = default);

    /// <summary>
    ///     Observable stream of state changes
    /// </summary>
    IObservable<StateChange<T>> ObserveState<T>(string key) where T : class;

    /// <summary>
    ///     Get all keys with a given prefix
    /// </summary>
    Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    ///     Save current state to temporary storage for later restoration
    /// </summary>
    Task SaveTemporaryState();

    /// <summary>
    ///     Restore previously saved temporary state
    /// </summary>
    Task RestoreTemporaryState();
}
