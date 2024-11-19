using System.Net;

using Chatty.Client.Connection;

namespace Chatty.Client.Exceptions;

/// <summary>
///     Exception thrown for connection-related errors
/// </summary>
public class ConnectionException : ChattyException
{
    public ConnectionException(
        string message,
        ConnectionState state,
        Exception? innerException = null)
        : base(message, "CONNECTION_ERROR", innerException)
    {
        ConnectionState = state;
        ReconnectAttempts = state.ReconnectAttempts;
        TimeSinceLastConnection = state.LastConnected.HasValue
            ? DateTime.UtcNow - state.LastConnected.Value
            : null;
    }

    public ConnectionException(
        string message,
        ConnectionState state,
        HttpStatusCode statusCode,
        Exception? innerException = null)
        : base(message, "CONNECTION_ERROR", statusCode, innerException)
    {
        ConnectionState = state;
        ReconnectAttempts = state.ReconnectAttempts;
        TimeSinceLastConnection = state.LastConnected.HasValue
            ? DateTime.UtcNow - state.LastConnected.Value
            : null;
    }

    /// <summary>
    ///     Connection state at the time of the error
    /// </summary>
    public ConnectionState ConnectionState { get; }

    /// <summary>
    ///     Number of reconnection attempts made
    /// </summary>
    public int ReconnectAttempts { get; }

    /// <summary>
    ///     Time since last successful connection
    /// </summary>
    public TimeSpan? TimeSinceLastConnection { get; }

    public static class ErrorCodes
    {
        public const string ConnectionFailed = "CONNECTION_FAILED";
        public const string ConnectionLost = "CONNECTION_LOST";
        public const string ReconnectionFailed = "RECONNECTION_FAILED";
        public const string CircuitBreakerOpen = "CIRCUIT_BREAKER_OPEN";
        public const string ServerUnreachable = "SERVER_UNREACHABLE";
        public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
        public const string ConnectionTimeout = "CONNECTION_TIMEOUT";
        public const string MaxRetriesExceeded = "MAX_RETRIES_EXCEEDED";
    }
}
