namespace Chatty.Client.Connection;

/// <summary>
///     Base class for connection events
/// </summary>
public abstract class ConnectionEvent(ConnectionState state)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public ConnectionState State { get; } = state;
}

/// <summary>
///     Event raised when connection state changes
/// </summary>
public class ConnectionStateChangedEvent(
    ConnectionState state,
    ConnectionStatus oldStatus,
    ConnectionStatus newStatus,
    Exception? error = null)
    : ConnectionEvent(state)
{
    public ConnectionStatus OldStatus { get; } = oldStatus;
    public ConnectionStatus NewStatus { get; } = newStatus;
    public Exception? Error { get; } = error;
}

/// <summary>
///     Event raised when connection latency changes significantly
/// </summary>
public class ConnectionLatencyChangedEvent(
    ConnectionState state,
    TimeSpan oldLatency,
    TimeSpan newLatency)
    : ConnectionEvent(state)
{
    public TimeSpan OldLatency { get; } = oldLatency;
    public TimeSpan NewLatency { get; } = newLatency;

    public double PercentageChange { get; } =
        (newLatency - oldLatency).TotalMilliseconds / oldLatency.TotalMilliseconds * 100;
}

/// <summary>
///     Event raised when reconnection attempts change
/// </summary>
public class ReconnectionAttemptEvent(
    ConnectionState state,
    int attemptNumber,
    TimeSpan nextRetryDelay,
    Exception? lastError = null)
    : ConnectionEvent(state)
{
    public int AttemptNumber { get; } = attemptNumber;
    public TimeSpan NextRetryDelay { get; } = nextRetryDelay;
    public Exception? LastError { get; } = lastError;
}

/// <summary>
///     Event raised when circuit breaker state changes
/// </summary>
public class CircuitBreakerEvent(
    ConnectionState state,
    CircuitBreakerState oldState,
    CircuitBreakerState newState,
    TimeSpan? durationUntilHalfOpen = null)
    : ConnectionEvent(state)
{
    public CircuitBreakerState OldState { get; } = oldState;
    public CircuitBreakerState NewState { get; } = newState;
    public TimeSpan? DurationUntilHalfOpen { get; } = durationUntilHalfOpen;
}

/// <summary>
///     Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    HalfOpen,
    Open
}
