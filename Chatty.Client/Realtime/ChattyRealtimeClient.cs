using System.Reactive.Subjects;
using System.Reflection;

using Chatty.Client.Connection;
using Chatty.Client.Models.Auth;
using Chatty.Client.Services;
using Chatty.Client.State;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Hubs;

using MessagePack;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chatty.Client.Realtime;

/// <summary>
///     Handles real-time communication with the Chatty backend using SignalR
/// </summary>
public sealed class ChattyRealtimeClient : IChattyRealtimeClient, IAsyncDisposable
{
    private static readonly TimeSpan MinEventInterval = TimeSpan.FromMilliseconds(100);
    private readonly IAuthService _authService;
    private readonly Subject<(Guid CallId, CallParticipantDto Participant)> _callParticipantJoined = new();
    private readonly Subject<(Guid CallId, CallParticipantDto Participant)> _callParticipantLeft = new();
    private readonly Subject<(Guid CallId, CallParticipantDto Participant, bool IsMuted)> _callParticipantMuted = new();

    private readonly Subject<(Guid CallId, CallParticipantDto Participant, bool IsVideoEnabled)> _callParticipantVideo =
        new();

    private readonly Subject<(Guid CallId, CallParticipantDto Participant, string SignalData)> _callSignalingReceived =
        new();

    private readonly Subject<(Guid CallId, CallDto Call)> _callStarted = new();
    private readonly Subject<(Guid ServerId, ChannelDto Channel)> _channelCreated = new();
    private readonly Subject<(Guid ServerId, Guid ChannelId)> _channelDeleted = new();

    // Channel member events
    private readonly Subject<(Guid ChannelId, UserDto User)> _channelMemberJoined = new();
    private readonly Subject<(Guid ChannelId, Guid UserId)> _channelMemberLeft = new();
    private readonly Subject<(Guid ChannelId, UserDto User)> _channelMemberUpdated = new();
    private readonly Subject<(Guid ServerId, ChannelDto Channel)> _channelUpdated = new();
    private readonly HubConnection _connection;
    private readonly Subject<Exception> _connectionError = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly IConnectionManager _connectionManager;

    // Connection state
    private readonly Subject<ConnectionState> _connectionState = new();
    private readonly Subject<(Guid MessageId, MessageReactionDto Reaction)> _directMessageReactionAdded = new();
    private readonly Subject<(Guid MessageId, Guid ReactionId, Guid UserId)> _directMessageReactionRemoved = new();
    private readonly Subject<DirectMessageDto> _directMessageReceived = new();

    private readonly HashSet<Guid> _joinedChannels = [];
    private readonly Dictionary<string, DateTime> _lastEventTime = new();
    private readonly ILogger<ChattyRealtimeClient> _logger;
    private readonly Subject<(Guid ChannelId, Guid MessageId)> _messageDeleted = new();
    private readonly Subject<(Guid ChannelId, MessageDto Message)> _messagePinned = new();

    // Reaction events
    private readonly Subject<(Guid ChannelId, Guid MessageId, MessageReactionDto Reaction)> _messageReactionAdded =
        new();

    private readonly Subject<(Guid ChannelId, Guid MessageId, Guid ReactionId, Guid UserId)> _messageReactionRemoved =
        new();

    // Event streams
    private readonly Subject<MessageDto> _messageReceived = new();
    private readonly Subject<(Guid MessageId, MessageDto Reply)> _messageReplied = new();
    private readonly Subject<(Guid ChannelId, Guid MessageId)> _messageUnpinned = new();
    private readonly Subject<(Guid ChannelId, MessageDto Message)> _messageUpdated = new();
    private readonly Subject<(Guid MessageId, MessageReactionDto Reaction)> _reactionAdded = new();
    private readonly Subject<(Guid MessageId, int ReactionCount)> _reactionCountUpdated = new();
    private readonly Subject<(Guid MessageId, MessageReactionDto Reaction)> _reactionRemoved = new();
    private readonly Subject<TimeSpan> _reconnectAttempt = new();
    private readonly Subject<(Guid MessageId, int ReplyCount)> _replyCountUpdated = new();
    private readonly Subject<(Guid ServerId, ServerRoleDto Role)> _roleCreated = new();
    private readonly Subject<(Guid ServerId, Guid RoleId)> _roleDeleted = new();
    private readonly Subject<(Guid ServerId, ServerRoleDto Role)> _roleUpdated = new();

    private readonly Subject<(Guid CallId, CallParticipantDto Participant, bool IsSharing)> _screenShareChanged = new();

    private readonly Subject<(Guid CallId, CallParticipantDto Participant, string StreamId)>
        _screenShareStarted = new();

    private readonly Subject<(Guid CallId, CallParticipantDto Participant)> _screenShareStopped = new();
    private readonly Subject<(Guid ServerId, ServerMemberDto Member)> _serverMemberJoined = new();
    private readonly Subject<(Guid ServerId, UserDto User)> _serverMemberLeft = new();
    private readonly IServiceProvider _services;
    private readonly IStateManager _stateManager;
    private readonly Subject<(Guid ChannelId, UserDto User)> _typingStarted = new();
    private readonly Subject<(Guid ChannelId, UserDto User)> _typingStopped = new();
    private readonly Subject<(Guid UserId, string Activity)> _userActivityChanged = new();
    private readonly Subject<(Guid UserId, bool IsOnline)> _userPresenceChanged = new();
    private readonly Subject<(Guid UserId, UserStatus Status)> _userStatusChanged = new();
    private readonly Subject<(Guid UserId, bool IsStreaming)> _userStreamingChanged = new();
    private readonly Subject<(Guid UserId, UserDto User)> _userUpdated = new();
    private bool _disposed;
    private SemaphoreSlim _sendThrottle = new(1, 1);

    public ChattyRealtimeClient(
        IServiceProvider services,
        ILogger<ChattyRealtimeClient> logger,
        IConnectionManager connectionManager,
        IStateManager stateManager,
        IAuthService authService,
        string baseUrl)
    {
        _services = services;
        _logger = logger;
        _connectionManager = connectionManager;
        _stateManager = stateManager;
        _authService = authService;

        var builder = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/chat", options =>
            {
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                options.WebSocketConfiguration = socket => { socket.KeepAliveInterval = TimeSpan.FromSeconds(30); };
                options.SkipNegotiation = true;
                options.AccessTokenProvider = async () =>
                {
                    var authState = await _stateManager.GetStateAsync<AuthState>(StateKeys.AuthToken());
                    if (authState?.Token == null)
                    {
                        _logger.LogWarning("Authentication token not found during reconnection");
                        await StopAsync();
                        return null;
                    }

                    return authState.Token;
                };
            })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithSecurity(MessagePackSecurity.UntrustedData);
            })
            .WithKeepAliveInterval(TimeSpan.FromSeconds(15))
            .WithAutomaticReconnect(new CustomRetryPolicy());

        _connection = builder.Build();
        SetupConnectionEvents();
        SetupMessageHandlers();
    }

    public async Task<List<ServerDto>> GetJoinedServersAsync(CancellationToken ct = default)
    {
        try
        {
            var servers = await _connection.InvokeAsync<List<ServerDto>>(
                HubMethods.GetJoinedServers,
                ct);

            return servers ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get joined servers");
            return [];
        }
    }

    public async Task PrepareForReconnection()
    {
        try
        {
            await _connectionLock.WaitAsync();
            await _stateManager.SaveTemporaryState();
            _sendThrottle.Dispose();
            _sendThrottle = new SemaphoreSlim(1, 1);
            _logger.LogInformation("Prepared for reconnection");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void SetupConnectionEvents()
    {
        _connection.Closed += OnConnectionClosed;
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;
    }

    private void SetupMessageHandlers()
    {
        foreach (var methodInfo in typeof(IChatHubClient).GetMethods())
        {
            var parameters = methodInfo.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var handler = CreateEventHandler(methodInfo.Name, parameters);
            _connection.On(methodInfo.Name, parameterTypes, args => handler(args));
        }
    }

    private Func<object?[], Task> CreateEventHandler(string eventName, ParameterInfo[] parameters) =>
        args =>
        {
            try
            {
                var field = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.EndsWith(eventName));

                if (field?.GetValue(this) is ISubject<object> subject &&
                    CreateEventArgs(parameters, args) is { } eventArgs)
                {
                    subject.OnNext(eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventName}", eventName);
                _connectionError.OnNext(ex);
            }

            return Task.CompletedTask;
        };

    private object? CreateEventArgs(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 1)
        {
            return args.ElementAtOrDefault(0);
        }

        var values = new object[args.Length];
        Array.Copy(args, values, args.Length);
        return Activator.CreateInstance(
            typeof(ValueTuple<,>).MakeGenericType(parameters.Select(p => p.ParameterType).ToArray()), values)!;
    }

    #region Rate Limiting

    private async Task ThrottleEventAsync(string eventName)
    {
        await _sendThrottle.WaitAsync();
        try
        {
            if (_lastEventTime.TryGetValue(eventName, out var lastTime))
            {
                var timeSinceLastEvent = DateTime.UtcNow - lastTime;
                if (timeSinceLastEvent < MinEventInterval)
                {
                    await Task.Delay(MinEventInterval - timeSinceLastEvent);
                }
            }

            _lastEventTime[eventName] = DateTime.UtcNow;
        }
        finally
        {
            _sendThrottle.Release();
        }
    }

    #endregion

    private async Task RestoreAfterReconnection()
    {
        try
        {
            await _connectionLock.WaitAsync();
            await _stateManager.RestoreTemporaryState();
            foreach (var channelId in _joinedChannels)
            {
                try
                {
                    await JoinChannelAsync(channelId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rejoin channel {ChannelId}", channelId);
                }
            }

            _logger.LogInformation("Restored state after reconnection");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private static class HubMethods
    {
        // Message Operations
        public const string SendMessage = "SendMessage";
        public const string SendDirectMessage = "SendDirectMessage";
        public const string UpdateMessage = "UpdateMessage";
        public const string UpdateDirectMessage = "UpdateDirectMessage";
        public const string DeleteMessage = "DeleteMessage";
        public const string DeleteDirectMessage = "DeleteDirectMessage";
        public const string ReplyToMessage = "ReplyToMessage";
        public const string PinMessage = "PinMessage";
        public const string UnpinMessage = "UnpinMessage";
        public const string AddReaction = "AddReaction";
        public const string RemoveReaction = "RemoveReaction";

        // Channel Operations
        public const string JoinChannel = "JoinChannel";
        public const string LeaveChannel = "LeaveChannel";
        public const string SendTypingIndicator = "SendTypingIndicator";
        public const string ClearTypingIndicator = "ClearTypingIndicator";

        // Server Operations
        public const string JoinServer = "JoinServer";
        public const string LeaveServer = "LeaveServer";
        public const string CreateRole = "CreateRole";
        public const string UpdateRole = "UpdateRole";
        public const string DeleteRole = "DeleteRole";
        public const string AssignRole = "AssignRole";
        public const string UnassignRole = "UnassignRole";
        public const string GetJoinedServers = "GetJoinedServers";

        // Voice Operations
        public const string StartCall = "StartCall";
        public const string JoinCall = "JoinCall";
        public const string LeaveCall = "LeaveCall";
        public const string MuteParticipant = "MuteParticipant";
        public const string EnableVideo = "EnableVideo";
        public const string SendSignalingMessage = "SendSignalingMessage";
        public const string StartScreenShare = "StartScreenShare";
        public const string StopScreenShare = "StopScreenShare";
        public const string RequestScreenSharePermission = "RequestScreenSharePermission";

        // User Operations
        public const string UpdateStatus = "UpdateStatus";
        public const string SetStreamingStatus = "SetStreamingStatus";
    }

    #region Connection Events

    public IObservable<ConnectionState> OnConnectionStateChanged => _connectionState;
    public IObservable<Exception> OnConnectionError => _connectionError;
    public IObservable<TimeSpan> OnReconnectAttempt => _reconnectAttempt;

    private Task OnConnectionClosed(Exception? ex)
    {
        _connectionState.OnNext(ConnectionState.Disconnected);
        if (ex != null)
        {
            _logger.LogError(ex, "SignalR connection closed with error");
            _connectionError.OnNext(ex);
        }

        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? ex)
    {
        _connectionState.OnNext(ConnectionState.Reconnecting);
        if (ex != null)
        {
            _logger.LogWarning(ex, "SignalR connection attempting to reconnect");
            _connectionError.OnNext(ex);
        }

        return Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        try
        {
            _connectionState.OnNext(ConnectionState.Connected);
            _logger.LogInformation("SignalR connection reestablished");

            // Restore state
            var state = await _stateManager.GetStateAsync<ChatState>(StateKeys.ChatState());
            if (state != null)
            {
                var tasks = new List<Task>();

                foreach (var channelId in state.ActiveChannels)
                {
                    tasks.Add(JoinChannelAsync(channelId));
                }

                foreach (var serverId in state.ActiveServers)
                {
                    tasks.Add(JoinServerAsync(serverId));
                }

                foreach (var callId in state.ActiveCalls)
                {
                    tasks.Add(JoinCallAsync(callId));
                }

                await Task.WhenAll(tasks);
            }

            // Restore user presence
            await UpdateStatusAsync(UserStatus.Online);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore connection state");
            _connectionError.OnNext(ex);
        }
    }

    #endregion

    #region Connection Management

    public async Task StartAsync()
    {
        ThrowIfDisposed();

        try
        {
            await _connection.StartAsync();
            _connectionState.OnNext(ConnectionState.Connected);
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SignalR connection");
            _connectionState.OnNext(ConnectionState.Failed);
            _connectionError.OnNext(ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        ThrowIfDisposed();

        try
        {
            await _connection.StopAsync();
            _connectionState.OnNext(ConnectionState.Disconnected);
            _logger.LogInformation("SignalR connection stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop SignalR connection");
            _connectionError.OnNext(ex);
            throw;
        }
    }

    private async Task<bool> TryReconnectAsync(int maxAttempts = 3)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await StartAsync();
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed", i + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        return false;
    }

    #endregion

    #region Hub Method Invocation

    private async Task InvokeWithRetryAsync(string methodName, params object?[] args)
    {
        ThrowIfDisposed();
        await ThrottleEventAsync(methodName);

        try
        {
            await _connection.InvokeAsync(methodName, args);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "Error invoking hub method {MethodName}", methodName);

            if (await TryReconnectAsync())
            {
                await _connection.InvokeAsync(methodName, args);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task<T> InvokeWithRetryAsync<T>(string methodName, params object[] args)
    {
        ThrowIfDisposed();
        await ThrottleEventAsync(methodName);

        try
        {
            return await _connection.InvokeAsync<T>(methodName, args);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "Error invoking hub method {MethodName}", methodName);

            if (await TryReconnectAsync())
            {
                return await _connection.InvokeAsync<T>(methodName, args);
            }

            throw;
        }
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Cleanup subscriptions
            foreach (var field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetValue(this) is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _sendThrottle.Dispose();

            // Stop connection
            if (_connection != null)
            {
                await StopAsync();
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing ChattyRealtimeClient");
        }
        finally
        {
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChattyRealtimeClient));
        }
    }

    #endregion

    #region Event Observables

    /// <summary>
    ///     Emits when a new message is received in a channel
    /// </summary>
    public IObservable<MessageDto> OnMessageReceived => _messageReceived;

    /// <summary>
    ///     Emits when a message is updated in a channel
    /// </summary>
    public IObservable<(Guid ChannelId, MessageDto Message)> OnMessageUpdated => _messageUpdated;

    /// <summary>
    ///     Emits when a message is deleted from a channel
    /// </summary>
    public IObservable<(Guid ChannelId, Guid MessageId)> OnMessageDeleted => _messageDeleted;

    /// <summary>
    ///     Emits when a direct message is received
    /// </summary>
    public IObservable<DirectMessageDto> OnDirectMessageReceived => _directMessageReceived;

    /// <summary>
    ///     Emits when a user starts typing in a channel
    /// </summary>
    public IObservable<(Guid ChannelId, UserDto User)> OnTypingStarted => _typingStarted;

    /// <summary>
    ///     Emits when a user stops typing in a channel
    /// </summary>
    public IObservable<(Guid ChannelId, UserDto User)> OnTypingStopped => _typingStopped;

    /// <summary>
    ///     Emits when a new member joins a server
    /// </summary>
    public IObservable<(Guid ServerId, ServerMemberDto Member)> OnServerMemberJoined => _serverMemberJoined;

    /// <summary>
    ///     Emits when a member leaves a server
    /// </summary>
    public IObservable<(Guid ServerId, UserDto User)> OnServerMemberLeft => _serverMemberLeft;

    /// <summary>
    ///     Emits when a user's online status changes
    /// </summary>
    public IObservable<(Guid UserId, bool IsOnline)> OnUserPresenceChanged => _userPresenceChanged;

    /// <summary>
    ///     Emits when a reaction is added to a message
    /// </summary>
    public IObservable<(Guid MessageId, MessageReactionDto Reaction)> OnReactionAdded => _reactionAdded;

    /// <summary>
    ///     Emits when a reaction is removed from a message
    /// </summary>
    public IObservable<(Guid MessageId, MessageReactionDto Reaction)> OnReactionRemoved => _reactionRemoved;

    /// <summary>
    ///     Emits when a message is pinned in a channel
    /// </summary>
    public IObservable<(Guid ChannelId, MessageDto Message)> OnMessagePinned => _messagePinned;

    /// <summary>
    ///     Emits when a message is unpinned from a channel
    /// </summary>
    public IObservable<(Guid ChannelId, Guid MessageId)> OnMessageUnpinned => _messageUnpinned;

    /// <summary>
    ///     Emits when a reply is added to a message
    /// </summary>
    public IObservable<(Guid MessageId, MessageDto Reply)> OnMessageReplied => _messageReplied;

    /// <summary>
    ///     Emits when the reply count for a message changes
    /// </summary>
    public IObservable<(Guid MessageId, int ReplyCount)> OnReplyCountUpdated => _replyCountUpdated;

    /// <summary>
    ///     Emits when the reaction count for a message changes
    /// </summary>
    public IObservable<(Guid MessageId, int ReactionCount)> OnReactionCountUpdated => _reactionCountUpdated;

    /// <summary>
    ///     Emits when a channel is created in a server
    /// </summary>
    public IObservable<(Guid ServerId, ChannelDto Channel)> OnChannelCreated => _channelCreated;

    /// <summary>
    ///     Emits when a channel is updated in a server
    /// </summary>
    public IObservable<(Guid ServerId, ChannelDto Channel)> OnChannelUpdated => _channelUpdated;

    /// <summary>
    ///     Emits when a channel is deleted from a server
    /// </summary>
    public IObservable<(Guid ServerId, Guid ChannelId)> OnChannelDeleted => _channelDeleted;

    /// <summary>
    ///     Emits when a role is created in a server
    /// </summary>
    public IObservable<(Guid ServerId, ServerRoleDto Role)> OnRoleCreated => _roleCreated;

    /// <summary>
    ///     Emits when a role is updated in a server
    /// </summary>
    public IObservable<(Guid ServerId, ServerRoleDto Role)> OnRoleUpdated => _roleUpdated;

    /// <summary>
    ///     Emits when a role is deleted from a server
    /// </summary>
    public IObservable<(Guid ServerId, Guid RoleId)> OnRoleDeleted => _roleDeleted;

    /// <summary>
    ///     Emits when a user is updated
    /// </summary>
    public IObservable<(Guid UserId, UserDto User)> OnUserUpdated => _userUpdated;

    /// <summary>
    ///     Emits when a user's status changes
    /// </summary>
    public IObservable<(Guid UserId, UserStatus Status)> OnUserStatusChanged => _userStatusChanged;

    /// <summary>
    ///     Emits when a user's activity changes
    /// </summary>
    public IObservable<(Guid UserId, string Activity)> OnUserActivityChanged => _userActivityChanged;

    /// <summary>
    ///     Emits when a user's streaming status changes
    /// </summary>
    public IObservable<(Guid UserId, bool IsStreaming)> OnUserStreamingChanged => _userStreamingChanged;

    /// <summary>
    ///     Emits when a call is started
    /// </summary>
    public IObservable<(Guid CallId, CallDto Call)> OnCallStarted => _callStarted;

    /// <summary>
    ///     Emits when a participant joins a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant)> OnCallParticipantJoined => _callParticipantJoined;

    /// <summary>
    ///     Emits when a participant leaves a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant)> OnCallParticipantLeft => _callParticipantLeft;

    /// <summary>
    ///     Emits when a participant's mute state changes in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant, bool IsMuted)> OnCallParticipantMuted =>
        _callParticipantMuted;

    /// <summary>
    ///     Emits when a participant's video state changes in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant, bool IsVideoEnabled)> OnCallParticipantVideo =>
        _callParticipantVideo;

    /// <summary>
    ///     Emits when a signaling message is received in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant, string SignalData)> OnCallSignalingReceived =>
        _callSignalingReceived;

    /// <summary>
    ///     Emits when a participant's screen sharing state changes in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant, bool IsSharing)> OnScreenShareChanged =>
        _screenShareChanged;

    /// <summary>
    ///     Emits when a participant starts screen sharing in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant, string StreamId)> OnScreenShareStarted =>
        _screenShareStarted;

    /// <summary>
    ///     Emits when a participant stops screen sharing in a call
    /// </summary>
    public IObservable<(Guid CallId, CallParticipantDto Participant)> OnScreenShareStopped => _screenShareStopped;

    /// <summary>
    ///     Emits when a channel member joins a channel
    /// </summary>
    public IObservable<(Guid ChannelId, UserDto User)> OnChannelMemberJoined => _channelMemberJoined;

    /// <summary>
    ///     Emits when a channel member leaves a channel
    /// </summary>
    public IObservable<(Guid ChannelId, Guid UserId)> OnChannelMemberLeft => _channelMemberLeft;

    /// <summary>
    ///     Emits when a channel member is updated in a channel
    /// </summary>
    public IObservable<(Guid ChannelId, UserDto User)> OnChannelMemberUpdated => _channelMemberUpdated;

    /// <summary>
    ///     Emits when a reaction is added to a channel message
    /// </summary>
    public IObservable<(Guid ChannelId, Guid MessageId, MessageReactionDto Reaction)> OnMessageReactionAdded =>
        _messageReactionAdded;

    /// <summary>
    ///     Emits when a reaction is removed from a channel message
    /// </summary>
    public IObservable<(Guid ChannelId, Guid MessageId, Guid ReactionId, Guid UserId)> OnMessageReactionRemoved =>
        _messageReactionRemoved;

    /// <summary>
    ///     Emits when a reaction is added to a direct message
    /// </summary>
    public IObservable<(Guid MessageId, MessageReactionDto Reaction)> OnDirectMessageReactionAdded =>
        _directMessageReactionAdded;

    /// <summary>
    ///     Emits when a reaction is removed from a direct message
    /// </summary>
    public IObservable<(Guid MessageId, Guid ReactionId, Guid UserId)> OnDirectMessageReactionRemoved =>
        _directMessageReactionRemoved;

    #endregion

    #region Channel Operations

    public async Task JoinChannelAsync(Guid channelId)
    {
        await _sendThrottle.WaitAsync();
        try
        {
            await _connection.InvokeAsync("JoinChannel", channelId);
            _joinedChannels.Add(channelId);
            _logger.LogInformation("Joined channel {ChannelId}", channelId);
        }
        finally
        {
            _sendThrottle.Release();
        }
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        await _sendThrottle.WaitAsync();
        try
        {
            await _connection.InvokeAsync("LeaveChannel", channelId);
            _joinedChannels.Remove(channelId);
            _logger.LogInformation("Left channel {ChannelId}", channelId);
        }
        finally
        {
            _sendThrottle.Release();
        }
    }

    public async Task SendTypingIndicatorAsync(Guid channelId)
    {
        ThrowIfDisposed();
        await ThrottleEventAsync(HubMethods.SendTypingIndicator);
        await InvokeWithRetryAsync(HubMethods.SendTypingIndicator, channelId);
    }

    #endregion

    #region Message Operations

    public async Task<MessageDto> SendMessageAsync(CreateMessageRequest request, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Sending message to channel {ChannelId}", request.ChannelId);
        return await InvokeWithRetryAsync<MessageDto>(HubMethods.SendMessage, request);
    }

    public async Task<DirectMessageDto> SendDirectMessageAsync(CreateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Sending direct message to user {UserId}", request.RecipientId);
        return await InvokeWithRetryAsync<DirectMessageDto>(HubMethods.SendDirectMessage, request);
    }

    public async Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Updating message {MessageId}", messageId);
        return await InvokeWithRetryAsync<MessageDto>(HubMethods.UpdateMessage, messageId, request);
    }

    public async Task<DirectMessageDto> UpdateDirectMessageAsync(Guid messageId, UpdateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Updating direct message {MessageId}", messageId);
        return await InvokeWithRetryAsync<DirectMessageDto>(HubMethods.UpdateDirectMessage, messageId, request);
    }

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Deleting message {MessageId}", messageId);
        await InvokeWithRetryAsync(HubMethods.DeleteMessage, messageId);
    }

    public async Task DeleteDirectMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Deleting direct message {MessageId}", messageId);
        await InvokeWithRetryAsync(HubMethods.DeleteDirectMessage, messageId);
    }

    public async Task<MessageDto> ReplyToMessageAsync(Guid messageId, ReplyMessageRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Replying to message {MessageId}", messageId);
        return await InvokeWithRetryAsync<MessageDto>(HubMethods.ReplyToMessage, messageId, request);
    }

    public async Task<bool> PinMessageAsync(Guid channelId, Guid messageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Pinning message {MessageId} in channel {ChannelId}", messageId, channelId);
        return await InvokeWithRetryAsync<bool>(HubMethods.PinMessage, channelId, messageId);
    }

    public async Task<bool> UnpinMessageAsync(Guid channelId, Guid messageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Unpinning message {MessageId} in channel {ChannelId}", messageId, channelId);
        return await InvokeWithRetryAsync<bool>(HubMethods.UnpinMessage, channelId, messageId);
    }

    public async Task AddReactionAsync(Guid messageId, string reaction)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Adding reaction {Reaction} to message {MessageId}", reaction, messageId);
        await InvokeWithRetryAsync(HubMethods.AddReaction, messageId, reaction);
    }

    public async Task RemoveReactionAsync(Guid messageId, string reaction)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Removing reaction {Reaction} from message {MessageId}", reaction, messageId);
        await InvokeWithRetryAsync(HubMethods.RemoveReaction, messageId, reaction);
    }

    #endregion

    #region Server Operations

    public async Task JoinServerAsync(Guid serverId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Joining server {ServerId}", serverId);
        await InvokeWithRetryAsync(HubMethods.JoinServer, serverId);
    }

    public async Task LeaveServerAsync(Guid serverId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Leaving server {ServerId}", serverId);
        await InvokeWithRetryAsync(HubMethods.LeaveServer, serverId);
    }

    #endregion

    #region Voice/Video Call Operations

    public async Task StartCallAsync(Guid channelId, bool withVideo = false)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Starting call in channel {ChannelId}", channelId);
        await InvokeWithRetryAsync(HubMethods.StartCall, channelId, withVideo);
    }

    public async Task JoinCallAsync(Guid callId, bool withVideo = false)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Joining call {CallId}", callId);
        await InvokeWithRetryAsync(HubMethods.JoinCall, callId, withVideo);
    }

    public async Task LeaveCallAsync(Guid callId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Leaving call {CallId}", callId);
        await InvokeWithRetryAsync(HubMethods.LeaveCall, callId);
    }

    public async Task MuteAsync(Guid callId, bool isMuted)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Setting mute state to {IsMuted} in call {CallId}", isMuted, callId);
        await InvokeWithRetryAsync(HubMethods.MuteParticipant, callId, isMuted);
    }

    public async Task EnableVideoAsync(Guid callId, bool isEnabled)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Setting video state to {IsEnabled} in call {CallId}", isEnabled, callId);
        await InvokeWithRetryAsync(HubMethods.EnableVideo, callId, isEnabled);
    }

    public async Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data)
    {
        ThrowIfDisposed();
        await InvokeWithRetryAsync(HubMethods.SendSignalingMessage, callId, peerId, type, data);
    }

    #endregion

    #region Screen Sharing Operations

    public async Task StartScreenShareAsync(Guid callId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Starting screen share in call {CallId}", callId);
        await InvokeWithRetryAsync(HubMethods.StartScreenShare, callId);
    }

    public async Task StopScreenShareAsync(Guid callId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Stopping screen share in call {CallId}", callId);
        await InvokeWithRetryAsync(HubMethods.StopScreenShare, callId);
    }

    public async Task RequestScreenSharePermissionAsync(Guid callId, Guid userId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Requesting screen share permission in call {CallId}", callId);
        await InvokeWithRetryAsync(HubMethods.RequestScreenSharePermission, callId, userId);
    }

    #endregion

    #region User Status Operations

    public async Task UpdateStatusAsync(UserStatus status, string? activity = null)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Updating user status to {Status}", status);
        await InvokeWithRetryAsync(HubMethods.UpdateStatus, status, activity);
    }

    public async Task SetStreamingStatusAsync(bool isStreaming, string? gameActivity = null)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Setting streaming status to {IsStreaming}", isStreaming);
        await InvokeWithRetryAsync(HubMethods.SetStreamingStatus, isStreaming, gameActivity);
    }

    #endregion

    #region Server Role Operations

    public async Task CreateRoleAsync(Guid serverId, CreateServerRoleRequest request)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Creating role in server {ServerId}", serverId);
        await InvokeWithRetryAsync(HubMethods.CreateRole, serverId, request);
    }

    public async Task UpdateRoleAsync(Guid serverId, Guid roleId, UpdateServerRoleRequest request)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Updating role {RoleId} in server {ServerId}", roleId, serverId);
        await InvokeWithRetryAsync(HubMethods.UpdateRole, serverId, roleId, request);
    }

    public async Task DeleteRoleAsync(Guid serverId, Guid roleId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Deleting role {RoleId} in server {ServerId}", roleId, serverId);
        await InvokeWithRetryAsync(HubMethods.DeleteRole, serverId, roleId);
    }

    public async Task AssignRoleAsync(Guid serverId, Guid userId, Guid roleId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Assigning role {RoleId} to user {UserId} in server {ServerId}", roleId, userId,
            serverId);
        await InvokeWithRetryAsync(HubMethods.AssignRole, serverId, userId, roleId);
    }

    public async Task UnassignRoleAsync(Guid serverId, Guid userId, Guid roleId)
    {
        ThrowIfDisposed();
        _logger.LogInformation("Unassigning role {RoleId} from user {UserId} in server {ServerId}", roleId, userId,
            serverId);
        await InvokeWithRetryAsync(HubMethods.UnassignRole, serverId, userId, roleId);
    }

    #endregion
}

public class CustomRetryPolicy : IRetryPolicy
{
    private readonly Random _random = new();

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // Implement exponential backoff with jitter
        var jitter = _random.Next(-2, 2);
        var delaySeconds = Math.Min(Math.Pow(2, retryContext.PreviousRetryCount), 60) + jitter;
        return TimeSpan.FromSeconds(delaySeconds);
    }
}

public enum ConnectionState
{
    Connecting,
    Connected,
    Reconnecting,
    Disconnected,
    Failed
}

public class ChatState
{
    public HashSet<Guid> ActiveChannels { get; set; } = [];
    public HashSet<Guid> ActiveServers { get; set; } = [];
    public HashSet<Guid> ActiveCalls { get; set; } = [];
}
