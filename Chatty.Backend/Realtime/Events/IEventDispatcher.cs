using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Realtime.Events;

public interface IEventDispatcher
{
    #region Notification Events

    Task DispatchBatchNotificationsAsync(IEnumerable<string> deviceTokens, string title, string message,
        Dictionary<string, string>? data = null);

    #endregion

    #region Message Events

    Task DispatchMessageReceivedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageUpdatedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageDeletedAsync(Guid channelId, Guid messageId);
    Task DispatchMessagePinnedAsync(Guid channelId, MessageDto message);
    Task DispatchMessageUnpinnedAsync(Guid channelId, Guid messageId);
    Task DispatchMessageReactionAddedAsync(Guid messageId, MessageReactionDto reaction);
    Task DispatchMessageReactionRemovedAsync(Guid channelId, Guid messageId, MessageReactionDto reaction);
    Task DispatchDirectMessageReactionAddedAsync(Guid messageId, MessageReactionDto reaction);
    Task DispatchDirectMessageReactionRemovedAsync(Guid messageId, Guid reactionId, Guid userId);
    Task DispatchMessageReplyAddedAsync(Guid messageId, MessageDto reply);
    Task DispatchMessageReplyCountUpdatedAsync(Guid messageId, int replyCount);
    Task DispatchMessageReactionCountUpdatedAsync(Guid messageId, int reactionCount);

    #endregion

    #region Direct Message Events

    Task DispatchDirectMessageReceivedAsync(DirectMessageDto message);
    Task DispatchDirectMessageUpdatedAsync(DirectMessageDto message);
    Task DispatchDirectMessageDeletedAsync(Guid messageId);

    #endregion

    #region Typing Events

    Task DispatchTypingStartedAsync(Guid channelId, UserDto user);
    Task DispatchTypingStoppedAsync(Guid channelId, UserDto user);
    Task DispatchDirectTypingStartedAsync(Guid userId, UserDto user);
    Task DispatchDirectTypingStoppedAsync(Guid userId, UserDto user);

    #endregion

    #region Channel Events

    Task DispatchChannelCreatedAsync(Guid serverId, ChannelDto channel);
    Task DispatchChannelUpdatedAsync(Guid serverId, ChannelDto channel);
    Task DispatchChannelDeletedAsync(Guid serverId, Guid channelId);
    Task DispatchChannelMemberJoinedAsync(Guid channelId, UserDto user);
    Task DispatchChannelMemberLeftAsync(Guid channelId, Guid userId);
    Task DispatchChannelMemberUpdatedAsync(Guid channelId, UserDto user);

    #endregion

    #region Server Events

    Task DispatchServerMemberJoinedAsync(Guid serverId, ServerMemberDto member);
    Task DispatchServerMemberLeftAsync(Guid serverId, Guid userId);
    Task DispatchServerMemberUpdatedAsync(Guid serverId, ServerMemberDto member);
    Task DispatchServerMemberKickedAsync(Guid serverId, Guid userId);
    Task DispatchMemberRoleUpdatedAsync(Guid serverId, Guid userId, Guid roleId);
    Task DispatchServerRoleCreatedAsync(Guid serverId, ServerRoleDto role);
    Task DispatchServerRoleUpdatedAsync(Guid serverId, ServerRoleDto role);
    Task DispatchServerRoleDeletedAsync(Guid serverId, Guid roleId);

    #endregion

    #region User Events

    Task DispatchUserUpdatedAsync(Guid userId, UserDto user);
    Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage);
    Task DispatchUserActivityChangedAsync(Guid userId, string activity);
    Task DispatchUserStreamingChangedAsync(Guid userId, bool isStreaming);

    #endregion

    #region Voice Events

    Task DispatchCallStartedAsync(CallDto call);
    Task DispatchCallEndedAsync(Guid callId);
    Task DispatchParticipantJoinedAsync(Guid callId, CallParticipantDto participant);
    Task DispatchParticipantLeftAsync(Guid callId, Guid userId);
    Task DispatchParticipantMutedAsync(Guid callId, Guid userId, bool isMuted);
    Task DispatchParticipantVideoEnabledAsync(Guid callId, Guid userId, bool isEnabled);
    Task DispatchSignalingMessageAsync(Guid callId, Guid userId, string type, string data);
    Task DispatchScreenShareChangedAsync(Guid callId, CallParticipantDto participant, bool isSharing);
    Task DispatchScreenShareStartedAsync(Guid callId, CallParticipantDto participant, string streamId);
    Task DispatchScreenShareStoppedAsync(Guid callId, CallParticipantDto participant);

    #endregion
}
