namespace Chatty.Client.Connection;

/// <summary>
///     Represents the current connection state
/// </summary>
public class ConnectionState
{
    public ConnectionStatus Status { get; internal set; }
    public DateTime? LastConnected { get; internal set; }
    public DateTime? LastDisconnected { get; internal set; }
    public int ReconnectAttempts { get; internal set; }
    public Exception? LastError { get; internal set; }
    public TimeSpan? Latency { get; internal set; }

    public bool IsConnected => Status == ConnectionStatus.Connected;
    public bool IsReconnecting => Status == ConnectionStatus.Reconnecting;
    public bool IsDisconnected => Status == ConnectionStatus.Disconnected;
}

/// <summary>
///     Connection status
/// </summary>
public enum ConnectionStatus
{
    Connected,
    Connecting,
    Reconnecting,
    Disconnected,
    Failed
}
