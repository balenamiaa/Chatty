using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
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
    Task OnSignalingMessage(Guid callId, Guid peerId, string type, string data);
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
}