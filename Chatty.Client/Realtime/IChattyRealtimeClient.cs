using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Client.Realtime;

/// <summary>
///     Interface for real-time communication with the Chatty backend
/// </summary>
public interface IChattyRealtimeClient : IAsyncDisposable
{
    Task PrepareForReconnection();

    #region Connection Events

    IObservable<ConnectionState> OnConnectionStateChanged { get; }
    IObservable<Exception> OnConnectionError { get; }
    IObservable<TimeSpan> OnReconnectAttempt { get; }

    #endregion

    #region Message Events

    IObservable<MessageDto> OnMessageReceived { get; }
    IObservable<(Guid ChannelId, MessageDto Message)> OnMessageUpdated { get; }
    IObservable<(Guid ChannelId, Guid MessageId)> OnMessageDeleted { get; }
    IObservable<DirectMessageDto> OnDirectMessageReceived { get; }
    IObservable<(Guid ChannelId, UserDto User)> OnTypingStarted { get; }
    IObservable<(Guid ChannelId, UserDto User)> OnTypingStopped { get; }
    IObservable<(Guid MessageId, MessageDto Reply)> OnMessageReplied { get; }
    IObservable<(Guid MessageId, int ReplyCount)> OnReplyCountUpdated { get; }
    IObservable<(Guid ChannelId, Guid MessageId, MessageReactionDto Reaction)> OnMessageReactionAdded { get; }
    IObservable<(Guid ChannelId, Guid MessageId, Guid ReactionId, Guid UserId)> OnMessageReactionRemoved { get; }
    IObservable<(Guid MessageId, MessageReactionDto Reaction)> OnDirectMessageReactionAdded { get; }
    IObservable<(Guid MessageId, Guid ReactionId, Guid UserId)> OnDirectMessageReactionRemoved { get; }
    IObservable<(Guid ChannelId, MessageDto Message)> OnMessagePinned { get; }
    IObservable<(Guid ChannelId, Guid MessageId)> OnMessageUnpinned { get; }

    #endregion

    #region Server Events

    IObservable<(Guid ServerId, ServerMemberDto Member)> OnServerMemberJoined { get; }
    IObservable<(Guid ServerId, UserDto User)> OnServerMemberLeft { get; }
    IObservable<(Guid ServerId, ChannelDto Channel)> OnChannelCreated { get; }
    IObservable<(Guid ServerId, ChannelDto Channel)> OnChannelUpdated { get; }
    IObservable<(Guid ServerId, Guid ChannelId)> OnChannelDeleted { get; }
    IObservable<(Guid ServerId, ServerRoleDto Role)> OnRoleCreated { get; }
    IObservable<(Guid ServerId, ServerRoleDto Role)> OnRoleUpdated { get; }
    IObservable<(Guid ServerId, Guid RoleId)> OnRoleDeleted { get; }

    #endregion

    #region Channel Member Events

    IObservable<(Guid ChannelId, UserDto User)> OnChannelMemberJoined { get; }
    IObservable<(Guid ChannelId, Guid UserId)> OnChannelMemberLeft { get; }
    IObservable<(Guid ChannelId, UserDto User)> OnChannelMemberUpdated { get; }

    #endregion

    #region User Events

    IObservable<(Guid UserId, bool IsOnline)> OnUserPresenceChanged { get; }
    IObservable<(Guid UserId, UserDto User)> OnUserUpdated { get; }
    IObservable<(Guid UserId, UserStatus Status)> OnUserStatusChanged { get; }
    IObservable<(Guid UserId, string Activity)> OnUserActivityChanged { get; }
    IObservable<(Guid UserId, bool IsStreaming)> OnUserStreamingChanged { get; }

    #endregion

    #region Voice/Video Events

    IObservable<(Guid CallId, CallDto Call)> OnCallStarted { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant)> OnCallParticipantJoined { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant)> OnCallParticipantLeft { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant, bool IsMuted)> OnCallParticipantMuted { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant, bool IsVideoEnabled)> OnCallParticipantVideo { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant, string SignalData)> OnCallSignalingReceived { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant, bool IsSharing)> OnScreenShareChanged { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant, string StreamId)> OnScreenShareStarted { get; }
    IObservable<(Guid CallId, CallParticipantDto Participant)> OnScreenShareStopped { get; }

    #endregion

    #region Connection Management

    Task StartAsync();
    Task StopAsync();

    #endregion

    #region Channel Operations

    Task JoinChannelAsync(Guid channelId);
    Task LeaveChannelAsync(Guid channelId);
    Task SendTypingIndicatorAsync(Guid channelId);

    #endregion

    #region Message Operations

    Task<MessageDto> SendMessageAsync(CreateMessageRequest request, CancellationToken ct = default);
    Task<DirectMessageDto> SendDirectMessageAsync(CreateDirectMessageRequest request, CancellationToken ct = default);
    Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageRequest request, CancellationToken ct = default);

    Task<DirectMessageDto> UpdateDirectMessageAsync(Guid messageId, UpdateDirectMessageRequest request,
        CancellationToken ct = default);

    Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default);
    Task DeleteDirectMessageAsync(Guid messageId, CancellationToken ct = default);
    Task<MessageDto> ReplyToMessageAsync(Guid messageId, ReplyMessageRequest request, CancellationToken ct = default);
    Task<bool> PinMessageAsync(Guid channelId, Guid messageId, CancellationToken ct = default);
    Task<bool> UnpinMessageAsync(Guid channelId, Guid messageId, CancellationToken ct = default);
    Task AddReactionAsync(Guid messageId, string reaction);
    Task RemoveReactionAsync(Guid messageId, string reaction);

    #endregion

    #region Server Operations

    Task JoinServerAsync(Guid serverId);
    Task LeaveServerAsync(Guid serverId);
    Task<List<ServerDto>> GetJoinedServersAsync(CancellationToken ct = default);

    #endregion

    #region Voice/Video Operations

    Task StartCallAsync(Guid channelId, bool withVideo = false);
    Task JoinCallAsync(Guid callId, bool withVideo = false);
    Task LeaveCallAsync(Guid callId);
    Task MuteAsync(Guid callId, bool isMuted);
    Task EnableVideoAsync(Guid callId, bool isEnabled);
    Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data);

    #endregion

    #region Screen Sharing Operations

    Task StartScreenShareAsync(Guid callId);
    Task StopScreenShareAsync(Guid callId);
    Task RequestScreenSharePermissionAsync(Guid callId, Guid userId);

    #endregion

    #region User Status Operations

    Task UpdateStatusAsync(UserStatus status, string? activity = null);
    Task SetStreamingStatusAsync(bool isStreaming, string? gameActivity = null);

    #endregion

    #region Server Role Operations

    Task CreateRoleAsync(Guid serverId, CreateServerRoleRequest request);
    Task UpdateRoleAsync(Guid serverId, Guid roleId, UpdateServerRoleRequest request);
    Task DeleteRoleAsync(Guid serverId, Guid roleId);
    Task AssignRoleAsync(Guid serverId, Guid userId, Guid roleId);
    Task UnassignRoleAsync(Guid serverId, Guid userId, Guid roleId);

    #endregion
}
