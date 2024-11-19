using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Hubs;

/// <summary>
///     Server-to-client real-time notifications
/// </summary>
public interface IChatHubClient
{
    // Message events
    Task OnMessageReceived(Guid channelId, MessageDto message);
    Task OnMessageUpdated(Guid channelId, MessageDto message);
    Task OnMessageDeleted(Guid channelId, Guid messageId);
    Task OnMessageReplied(Guid messageId, MessageDto reply);
    Task OnReplyCountUpdated(Guid messageId, int replyCount);
    Task OnReactionCountUpdated(Guid messageId, int reactionCount);
    Task OnMessagePinned(Guid channelId, MessageDto message);
    Task OnMessageUnpinned(Guid channelId, Guid messageId);

    // Direct message events
    Task OnDirectMessageReceived(DirectMessageDto message);
    Task OnDirectMessageUpdated(DirectMessageDto message);
    Task OnDirectMessageDeleted(Guid messageId);

    // Typing events
    Task OnTypingStarted(Guid channelId, UserDto user);
    Task OnTypingStopped(Guid channelId, UserDto user);
    Task OnDirectTypingStarted(Guid userId, UserDto user);
    Task OnDirectTypingStopped(Guid userId, UserDto user);

    // Channel member events
    Task OnChannelMemberJoined(Guid channelId, UserDto user);
    Task OnChannelMemberLeft(Guid channelId, Guid userId);
    Task OnChannelMemberUpdated(Guid channelId, UserDto user);

    // Server events
    Task OnChannelCreated(Guid serverId, ChannelDto channel);
    Task OnChannelUpdated(Guid serverId, ChannelDto channel);
    Task OnChannelDeleted(Guid serverId, Guid channelId);
    Task OnRoleCreated(Guid serverId, ServerRoleDto role);
    Task OnRoleUpdated(Guid serverId, ServerRoleDto role);
    Task OnRoleDeleted(Guid serverId, Guid roleId);

    // Presence events
    Task OnUserPresenceChanged(Guid userId, UserStatus status, string? statusMessage);
    Task OnUserUpdated(Guid userId, UserDto user);
    Task OnUserActivityChanged(Guid userId, string activity);
    Task OnUserStreamingChanged(Guid userId, bool isStreaming);

    // Voice events
    Task OnCallStarted(CallDto call);
    Task OnCallEnded(Guid callId);
    Task OnParticipantJoined(Guid callId, CallParticipantDto participant);
    Task OnParticipantLeft(Guid callId, Guid userId);
    Task OnParticipantMuted(Guid callId, Guid userId, bool isMuted);
    Task OnParticipantVideoEnabled(Guid callId, Guid userId, bool isEnabled);
    Task OnSignalingMessage(Guid peerId, string type, string data);
    Task OnScreenShareChanged(Guid callId, CallParticipantDto participant, bool isSharing);
    Task OnScreenShareStarted(Guid callId, CallParticipantDto participant, string streamId);
    Task OnScreenShareStopped(Guid callId, CallParticipantDto participant);

    // Server member events
    Task OnMemberJoined(Guid serverId, ServerMemberDto member);
    Task OnMemberLeft(Guid serverId, Guid userId);
    Task OnMemberUpdated(Guid serverId, ServerMemberDto member);
    Task OnMemberRoleUpdated(Guid serverId, Guid userId, Guid roleId);

    // Notification events
    Task OnNotification(string title, string message);
    Task OnPushNotification(string title, string message, Dictionary<string, string>? data = null);

    // Reaction events
    Task OnMessageReactionAdded(Guid? channelId, Guid messageId, MessageReactionDto reaction);
    Task OnMessageReactionRemoved(Guid? channelId, Guid messageId, Guid reactionId, Guid userId);
    Task OnDirectMessageReactionAdded(Guid messageId, MessageReactionDto reaction);
    Task OnDirectMessageReactionRemoved(Guid messageId, Guid reactionId, Guid userId);
}
