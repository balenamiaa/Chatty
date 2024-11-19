namespace Chatty.Client.Connection;

/// <summary>
///     Manages connection state and reconnection logic
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    ///     Observable stream of connection state changes
    /// </summary>
    IObservable<ConnectionState> ConnectionState { get; }

    /// <summary>
    ///     Observable stream of connection errors
    /// </summary>
    IObservable<Exception> ConnectionError { get; }

    /// <summary>
    ///     Observable stream of reconnection attempts
    /// </summary>
    IObservable<TimeSpan> ReconnectAttempt { get; }

    /// <summary>
    ///     Connect to the server
    /// </summary>
    Task ConnectAsync(string token, CancellationToken ct = default);

    /// <summary>
    ///     Disconnect from the server
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);
}
