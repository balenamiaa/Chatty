using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Notifications;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Hubs;

/// <summary>
/// Server-to-client real-time notifications
/// </summary>
public interface IChatHubClient
{
    // Message events
    Task OnMessageReceived(Guid channelId, MessageDto message);
    Task OnMessageUpdated(Guid channelId, MessageDto message);
    Task OnMessageDeleted(Guid channelId, Guid messageId);

    // Direct message events
    Task OnDirectMessageReceived(DirectMessageDto message);
    Task OnDirectMessageUpdated(DirectMessageDto message);
    Task OnDirectMessageDeleted(Guid messageId);

    // Typing events
    Task OnTypingStarted(Guid channelId, UserDto user);
    Task OnTypingStopped(Guid channelId, UserDto user);
    Task OnDirectTypingStarted(Guid userId, UserDto user);
    Task OnDirectTypingStopped(Guid userId, UserDto user);

    // Presence events
    Task OnUserPresenceChanged(Guid userId, UserStatus status, string? statusMessage);
    Task OnUserOnlineStateChanged(Guid userId, bool isOnline);

    // Voice events
    Task OnCallStarted(CallDto call);
    Task OnCallEnded(Guid callId);
    Task OnParticipantJoined(Guid callId, CallParticipantDto participant);
    Task OnParticipantLeft(Guid callId, Guid userId);
    Task OnParticipantMuted(Guid callId, Guid userId, bool isMuted);
    Task OnParticipantVideoEnabled(Guid callId, Guid userId, bool isEnabled);
    Task OnSignalingMessage(Guid peerId, string type, string data);

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

/// <summary>
/// Client-to-server real-time commands
/// </summary>
public interface IChatHub
{
    // Connection management
    Task JoinChannelAsync(Guid channelId);
    Task LeaveChannelAsync(Guid channelId);

    // Typing indicators
    Task StartTypingAsync(Guid channelId);
    Task StopTypingAsync(Guid channelId);
    Task StartDirectTypingAsync(Guid userId);
    Task StopDirectTypingAsync(Guid userId);

    // Presence
    Task UpdatePresenceAsync(UserStatus status, string? statusMessage = null);

    // Voice/Video signaling
    Task JoinCallAsync(Guid callId, bool withVideo);
    Task LeaveCallAsync(Guid callId);
    Task MuteAsync(Guid callId, bool muted);
    Task EnableVideoAsync(Guid callId, bool enabled);
    Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data);

    // Message operations
    Task SendMessageAsync(CreateMessageRequest request);
    Task UpdateMessageAsync(Guid messageId, UpdateMessageRequest request);
    Task DeleteMessageAsync(Guid messageId);

    // Direct message operations
    Task SendDirectMessageAsync(CreateDirectMessageRequest request);
    Task UpdateDirectMessageAsync(Guid messageId, UpdateDirectMessageRequest request);
    Task DeleteDirectMessageAsync(Guid messageId);

    // Server member operations
    Task JoinServerAsync(Guid serverId);
    Task LeaveServerAsync(Guid serverId);
    Task UpdateMemberRoleAsync(Guid serverId, Guid userId, Guid roleId);
    Task KickMemberAsync(Guid serverId, Guid userId);

    // Notification operations
    Task SubscribeToNotificationsAsync(string deviceToken, DeviceType deviceType);
    Task UnsubscribeFromNotificationsAsync(string deviceToken);
    Task UpdateNotificationPreferencesAsync(NotificationPreferences preferences);
}
