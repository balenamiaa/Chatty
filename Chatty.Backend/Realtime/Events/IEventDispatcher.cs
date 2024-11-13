using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Realtime.Events;

public interface IEventDispatcher
{
    Task DispatchMessageReceivedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageUpdatedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageDeletedAsync(Guid channelId, Guid messageId);

    Task DispatchDirectMessageReceivedAsync(DirectMessageDto message);
    Task DispatchDirectMessageUpdatedAsync(DirectMessageDto message);
    Task DispatchDirectMessageDeletedAsync(Guid messageId);

    Task DispatchTypingStartedAsync(Guid channelId, UserDto user);
    Task DispatchTypingStoppedAsync(Guid channelId, UserDto user);
    Task DispatchDirectTypingStartedAsync(Guid userId, UserDto user);
    Task DispatchDirectTypingStoppedAsync(Guid userId, UserDto user);

    Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage);
    Task DispatchUserOnlineStateChangedAsync(Guid userId, bool isOnline);

    Task DispatchCallStartedAsync(CallDto call);
    Task DispatchCallEndedAsync(Guid callId);
    Task DispatchParticipantJoinedAsync(Guid callId, CallParticipantDto participant);
    Task DispatchParticipantLeftAsync(Guid callId, Guid userId);
    Task DispatchParticipantMutedAsync(Guid callId, Guid userId, bool isMuted);
    Task DispatchParticipantVideoEnabledAsync(Guid callId, Guid userId, bool isEnabled);
}