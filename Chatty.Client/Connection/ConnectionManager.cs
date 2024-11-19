using System.Reactive.Linq;
using System.Reactive.Subjects;

using Chatty.Client.Realtime;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Chatty.Client.Connection;

/// <summary>
///     Implementation of connection manager
/// </summary>
public class ConnectionManager(
    string hubUrl,
    IChattyRealtimeClient realtimeClient,
    ILogger<ConnectionManager> logger)
    : IConnectionManager, IDisposable
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];

    private readonly Subject<Exception> _connectionError = new();

    private readonly Subject<ConnectionEvent> _connectionEvents = new();

    private readonly BehaviorSubject<ConnectionState> _connectionState = new(new ConnectionState
        { Status = ConnectionStatus.Disconnected });

    private readonly Subject<TimeSpan> _reconnectAttempt = new();
    private readonly CancellationTokenSource _reconnectCts = new();
    private HubConnection? _hubConnection;
    private bool _isDisposed;
    public IObservable<ConnectionEvent> ConnectionEvents => _connectionEvents.AsObservable();

    public async Task ConnectAsync(string token, CancellationToken ct = default)
    {
        if (_hubConnection != null)
        {
            logger.LogWarning("Already connected or connecting");
            return;
        }

        var oldState = _connectionState.Value;
        var newState = new ConnectionState { Status = ConnectionStatus.Connecting };
        try
        {
            _connectionState.OnNext(newState);
            _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status));

            // Create hub connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.AccessTokenProvider = () => Task.FromResult<string?>(token); })
                .WithAutomaticReconnect(RetryDelays)
                .Build();

            // Set up connection event handlers
            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;

            // Register hub event handlers
            RegisterHubEventHandlers(_hubConnection);

            // Start connection
            await _hubConnection.StartAsync(ct);
            oldState = _connectionState.Value;
            newState = new ConnectionState { Status = ConnectionStatus.Connected, LastConnected = DateTime.UtcNow };
            _connectionState.OnNext(newState);
            _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status));
        }
        catch (Exception ex)
        {
            oldState = _connectionState.Value;
            newState = new ConnectionState
            {
                Status = ConnectionStatus.Failed,
                LastError = ex,
                LastDisconnected = DateTime.UtcNow
            };
            _connectionState.OnNext(newState);
            _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status, ex));
            logger.LogError(ex, "Failed to connect to hub");
            _connectionError.OnNext(ex);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_hubConnection == null)
        {
            return;
        }

        try
        {
            await _hubConnection.StopAsync(ct);
            _hubConnection.Closed -= OnConnectionClosed;
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;
            _hubConnection = null;
            var oldState = _connectionState.Value;
            var newState = new ConnectionState
            {
                Status = ConnectionStatus.Disconnected,
                LastDisconnected = DateTime.UtcNow,
                LastConnected = oldState.LastConnected
            };
            _connectionState.OnNext(newState);
            _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disconnecting from hub");
            throw;
        }
    }

    public IObservable<ConnectionState> ConnectionState => _connectionState.AsObservable();
    public IObservable<Exception> ConnectionError => _connectionError.AsObservable();
    public IObservable<TimeSpan> ReconnectAttempt => _reconnectAttempt.AsObservable();

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Cancel any ongoing reconnection attempts
        _reconnectCts.Cancel();
        _reconnectCts.Dispose();

        // Cleanup subjects
        _connectionState.Dispose();
        _connectionEvents.Dispose();
        _connectionError.Dispose();
        _reconnectAttempt.Dispose();

        // Cleanup hub connection
        if (_hubConnection != null)
        {
            _hubConnection.Closed -= OnConnectionClosed;
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;
            _hubConnection.DisposeAsync().AsTask().Wait();
            _hubConnection = null;
        }
    }

    private void RegisterHubEventHandlers(HubConnection hub)
    {
        // Connection events are handled by ConnectionManager
        hub.Closed += OnConnectionClosed;
        hub.Reconnecting += OnReconnecting;
        hub.Reconnected += OnReconnected;

        logger.LogInformation("Registered hub event handlers");
    }

    private Task OnConnectionClosed(Exception? ex)
    {
        if (ex != null)
        {
            logger.LogError(ex, "Connection closed with error");
            _connectionError.OnNext(ex);
        }

        var oldState = _connectionState.Value;
        var newState = new ConnectionState
        {
            Status = ConnectionStatus.Disconnected,
            LastError = ex,
            LastDisconnected = DateTime.UtcNow,
            LastConnected = oldState.LastConnected,
            ReconnectAttempts = oldState.ReconnectAttempts
        };
        _connectionState.OnNext(newState);
        _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status, ex));
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? ex)
    {
        logger.LogInformation(ex, "Attempting to reconnect");
        var oldState = _connectionState.Value;
        var newState = new ConnectionState
        {
            Status = ConnectionStatus.Reconnecting,
            LastError = ex,
            LastDisconnected = DateTime.UtcNow,
            LastConnected = oldState.LastConnected,
            ReconnectAttempts = oldState.ReconnectAttempts + 1
        };
        _connectionState.OnNext(newState);
        _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status, ex));

        // Calculate exponential backoff delay with jitter for reconnection
        var baseDelay = Math.Min(Math.Pow(2, newState.ReconnectAttempts), 60);
        var jitter = Random.Shared.NextDouble() * 0.1 * baseDelay; // 10% jitter
        var nextRetryDelay = TimeSpan.FromSeconds(baseDelay + jitter);

        _connectionEvents.OnNext(new ReconnectionAttemptEvent(
            newState,
            newState.ReconnectAttempts,
            nextRetryDelay,
            ex));

        // Notify realtime client to prepare for reconnection
        realtimeClient.PrepareForReconnection();

        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        logger.LogInformation("Reconnected with connection ID: {ConnectionId}", connectionId);
        var oldState = _connectionState.Value;
        var newState = new ConnectionState
        {
            Status = ConnectionStatus.Connected,
            LastConnected = DateTime.UtcNow,
            LastDisconnected = oldState.LastDisconnected,
            ReconnectAttempts = 0
        };
        _connectionState.OnNext(newState);
        _connectionEvents.OnNext(new ConnectionStateChangedEvent(newState, oldState.Status, newState.Status));
        return Task.CompletedTask;
    }
}
