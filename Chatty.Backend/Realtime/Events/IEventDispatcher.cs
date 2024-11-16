using System.Collections.Generic;
using System.Threading.Tasks;

using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Realtime.Events;

public interface IEventDispatcher
{
    // Message events
    Task DispatchMessageReceivedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageUpdatedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageDeletedAsync(Guid channelId, Guid messageId);

    // Direct message events
    Task DispatchDirectMessageReceivedAsync(DirectMessageDto message);
    Task DispatchDirectMessageUpdatedAsync(DirectMessageDto message);
    Task DispatchDirectMessageDeletedAsync(Guid messageId);

    // Typing events
    Task DispatchTypingStartedAsync(Guid channelId, UserDto user);
    Task DispatchTypingStoppedAsync(Guid channelId, UserDto user);
    Task DispatchDirectTypingStartedAsync(Guid userId, UserDto user);
    Task DispatchDirectTypingStoppedAsync(Guid userId, UserDto user);

    // Presence events
    Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage);
    Task DispatchUserOnlineStateChangedAsync(Guid userId, bool isOnline);

    // Voice events
    Task DispatchCallStartedAsync(CallDto call);
    Task DispatchCallEndedAsync(Guid callId);
    Task DispatchParticipantJoinedAsync(Guid callId, CallParticipantDto participant);
    Task DispatchParticipantLeftAsync(Guid callId, Guid userId);
    Task DispatchParticipantMutedAsync(Guid callId, Guid userId, bool isMuted);
    Task DispatchParticipantVideoEnabledAsync(Guid callId, Guid userId, bool isEnabled);
    Task DispatchSignalingMessageAsync(Guid callId, Guid userId, string type, string data);

    // Server member events
    Task DispatchServerMemberJoinedAsync(Guid serverId, ServerMemberDto member);
    Task DispatchServerMemberLeftAsync(Guid serverId, Guid userId);
    Task DispatchServerMemberUpdatedAsync(Guid serverId, ServerMemberDto member);
    Task DispatchServerMemberKickedAsync(Guid serverId, Guid userId);

    // Notification events
    Task DispatchBatchNotificationsAsync(IEnumerable<string> deviceTokens, string title, string message, Dictionary<string, string>? data = null);
}
