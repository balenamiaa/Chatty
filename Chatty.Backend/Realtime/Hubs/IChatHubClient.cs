using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Realtime.Hubs;

public interface IChatHubClient
{
    Task OnMessageReceived(Guid channelId, MessageDto message);
    Task OnMessageUpdated(Guid channelId, MessageDto message);
    Task OnMessageDeleted(Guid channelId, Guid messageId);
    Task OnDirectMessageReceived(DirectMessageDto message);
    Task OnDirectMessageUpdated(DirectMessageDto message);
    Task OnDirectMessageDeleted(Guid messageId);
    Task OnTypingStarted(Guid channelId, UserDto user);
    Task OnTypingStopped(Guid channelId, UserDto user);
    Task OnDirectTypingStarted(Guid userId, UserDto user);
    Task OnDirectTypingStopped(Guid userId, UserDto user);
    Task OnUserPresenceChanged(Guid userId, UserStatus status, string? statusMessage);
    Task OnUserOnlineStateChanged(Guid userId, bool isOnline);
    Task OnCallStarted(CallDto call);
    Task OnCallEnded(Guid callId);
    Task OnParticipantJoined(Guid callId, CallParticipantDto participant);
    Task OnParticipantLeft(Guid callId, Guid userId);
    Task OnParticipantMuted(Guid callId, Guid userId, bool isMuted);
    Task OnParticipantVideoEnabled(Guid callId, Guid userId, bool isEnabled);
    Task OnSignalingMessage(Guid callId, Guid userId, string type, string data);
    Task OnNotification(string title, string message);
}