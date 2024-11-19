namespace Chatty.Client.State;

/// <summary>
///     Represents a change in state
/// </summary>
public class StateChange<T>(
    string key,
    T? oldValue,
    T? newValue,
    StateChangeType changeType)
    where T : class
{
    /// <summary>
    ///     Key of the state that changed
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    ///     Previous value
    /// </summary>
    public T? OldValue { get; } = oldValue;

    /// <summary>
    ///     New value
    /// </summary>
    public T? NewValue { get; } = newValue;

    /// <summary>
    ///     Type of change
    /// </summary>
    public StateChangeType ChangeType { get; } = changeType;

    /// <summary>
    ///     Timestamp of the change
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
///     Type of state change
/// </summary>
public enum StateChangeType
{
    /// <summary>
    ///     State was created
    /// </summary>
    Created,

    /// <summary>
    ///     State was updated
    /// </summary>
    Updated,

    /// <summary>
    ///     State was deleted
    /// </summary>
    Deleted,

    /// <summary>
    ///     State was cleared
    /// </summary>
    Cleared
}
