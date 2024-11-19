using Chatty.Backend.Data;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Hubs;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Realtime.Events;

public sealed class EventDispatcher(
    IHubContext<ChatHub, IChatHubClient> hubContext,
    ChattyDbContext context,
    IConnectionTracker connectionTracker,
    ILogger<EventDispatcher> logger)
    : IEventDispatcher
{
    public async Task DispatchMessageReceivedAsync(Guid channelId, MessageDto message)
    {
        try
        {
            // Broadcast to channel group
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageReceived(channelId, message);

            // Also notify channel members individually for their notifications
            var channelMembers = await context.ChannelMembers
                .Where(m => m.ChannelId == channelId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in channelMembers)
            {
                var connections = await connectionTracker.GetConnectionsAsync(memberId);
                if (connections.Any())
                {
                    await hubContext.Clients
                        .Clients(connections)
                        .OnNotification(
                            "New Message",
                            $"New message in channel from {message.Sender.Username}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch message received event");
        }
    }

    public async Task DispatchMessageUpdatedAsync(Guid channelId, MessageDto message)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageUpdated(channelId, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch message updated event");
        }
    }

    public async Task DispatchMessageDeletedAsync(Guid channelId, Guid messageId)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageDeleted(channelId, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch message deleted event");
        }
    }

    public async Task DispatchDirectMessageReceivedAsync(DirectMessageDto message)
    {
        try
        {
            await DispatchToUserAsync(message.Recipient.Id,
                client => client.OnDirectMessageReceived(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct message received event");
        }
    }

    public async Task DispatchDirectMessageUpdatedAsync(DirectMessageDto message)
    {
        try
        {
            await DispatchToUserAsync(message.Recipient.Id,
                client => client.OnDirectMessageUpdated(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct message updated event");
        }
    }

    public async Task DispatchDirectMessageDeletedAsync(Guid messageId)
    {
        try
        {
            // Note: This needs to be dispatched to both sender and recipient
            // You might want to modify the method signature to include both IDs
            await hubContext.Clients.All.OnDirectMessageDeleted(messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct message deleted event");
        }
    }

    public async Task DispatchTypingStartedAsync(Guid channelId, UserDto user)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnTypingStarted(channelId, user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch typing started event");
        }
    }

    public async Task DispatchTypingStoppedAsync(Guid channelId, UserDto user)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnTypingStopped(channelId, user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch typing stopped event");
        }
    }

    public async Task DispatchDirectTypingStartedAsync(Guid userId, UserDto user)
    {
        try
        {
            await DispatchToUserAsync(userId,
                client => client.OnDirectTypingStarted(userId, user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct typing started event");
        }
    }

    public async Task DispatchDirectTypingStoppedAsync(Guid userId, UserDto user)
    {
        try
        {
            await DispatchToUserAsync(userId,
                client => client.OnDirectTypingStopped(userId, user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct typing stopped event");
        }
    }

    public Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage) =>
        hubContext.Clients.All.OnUserPresenceChanged(userId, status, statusMessage);

    public async Task DispatchCallStartedAsync(CallDto call)
    {
        try
        {
            if (call.ChannelId.HasValue)
            {
                await hubContext.Clients
                    .Group($"channel_{call.ChannelId}")
                    .OnCallStarted(call);
            }
            else
            {
                // Direct call
                await DispatchToUserAsync(call.Participants.First().User.Id,
                    client => client.OnCallStarted(call));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch call started event");
        }
    }

    public async Task DispatchCallEndedAsync(Guid callId)
    {
        try
        {
            await hubContext.Clients.All.OnCallEnded(callId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch call ended event");
        }
    }

    public async Task DispatchParticipantJoinedAsync(Guid callId, CallParticipantDto participant)
    {
        try
        {
            await hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantJoined(callId, participant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch participant joined event");
        }
    }

    public async Task DispatchParticipantLeftAsync(Guid callId, Guid userId)
    {
        try
        {
            await hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantLeft(callId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch participant left event");
        }
    }

    public async Task DispatchParticipantMutedAsync(Guid callId, Guid userId, bool isMuted)
    {
        try
        {
            await hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantMuted(callId, userId, isMuted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch participant muted event");
        }
    }

    public async Task DispatchParticipantVideoEnabledAsync(Guid callId, Guid userId, bool isEnabled)
    {
        try
        {
            await hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantVideoEnabled(callId, userId, isEnabled);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch participant video enabled event");
        }
    }

    public async Task DispatchBatchNotificationsAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var tasks = deviceTokens.Select(token =>
                hubContext.Clients
                    .Client(token)
                    .OnNotification(title, message));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch batch notifications");
        }
    }

    public Task DispatchChannelMemberJoinedAsync(Guid channelId, UserDto user) =>
        hubContext.Clients
            .Group($"channel_{channelId}")
            .OnChannelMemberJoined(channelId, user);

    public Task DispatchChannelMemberLeftAsync(Guid channelId, Guid userId) =>
        hubContext.Clients
            .Group($"channel_{channelId}")
            .OnChannelMemberLeft(channelId, userId);

    public Task DispatchChannelMemberUpdatedAsync(Guid channelId, UserDto user) =>
        hubContext.Clients
            .Group($"channel_{channelId}")
            .OnChannelMemberUpdated(channelId, user);

    public Task DispatchServerMemberJoinedAsync(Guid serverId, ServerMemberDto member) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnMemberJoined(serverId, member);

    public Task DispatchServerMemberLeftAsync(Guid serverId, Guid userId) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnMemberLeft(serverId, userId);

    public Task DispatchServerMemberUpdatedAsync(Guid serverId, ServerMemberDto member) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnMemberUpdated(serverId, member);

    public Task DispatchServerMemberKickedAsync(Guid serverId, Guid userId)
    {
        try
        {
            // Notify server members
            return hubContext.Clients
                .Group($"server_{serverId}")
                .OnMemberLeft(serverId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch server member kicked event");
            return Task.CompletedTask;
        }
    }

    public async Task DispatchSignalingMessageAsync(Guid userId, Guid senderId, string type, string data)
    {
        try
        {
            // For signaling messages, we don't need a callId as it's part of the data
            await DispatchToUserAsync(userId,
                client => client.OnSignalingMessage(senderId, type, data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch signaling message");
        }
    }

    public Task DispatchMessageReactionAddedAsync(Guid messageId, MessageReactionDto reaction) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnMessageReactionAdded(null, messageId, reaction);

    public Task DispatchMessageReactionRemovedAsync(Guid channelId, Guid messageId, MessageReactionDto reaction) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnMessageReactionRemoved(channelId, messageId, reaction.Id, reaction.User.Id);

    public Task DispatchDirectMessageReactionAddedAsync(Guid messageId, MessageReactionDto reaction) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnDirectMessageReactionAdded(messageId, reaction);

    public Task DispatchDirectMessageReactionRemovedAsync(Guid messageId, Guid reactionId, Guid userId) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnDirectMessageReactionRemoved(messageId, reactionId, userId);

    public Task DispatchScreenShareStartedAsync(Guid callId, CallParticipantDto participant, string streamId) =>
        hubContext.Clients
            .Group($"call_{callId}")
            .OnScreenShareStarted(callId, participant, streamId);

    public Task DispatchScreenShareStoppedAsync(Guid callId, CallParticipantDto participant) =>
        hubContext.Clients
            .Group($"call_{callId}")
            .OnScreenShareStopped(callId, participant);

    public Task DispatchScreenShareChangedAsync(Guid callId, CallParticipantDto participant, bool isSharing) =>
        hubContext.Clients
            .Group($"call_{callId}")
            .OnScreenShareChanged(callId, participant, isSharing);

    public Task DispatchMemberRoleUpdatedAsync(Guid serverId, Guid userId, Guid roleId) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnMemberRoleUpdated(serverId, userId, roleId);

    public Task DispatchChannelCreatedAsync(Guid serverId, ChannelDto channel) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnChannelCreated(serverId, channel);

    public Task DispatchChannelUpdatedAsync(Guid serverId, ChannelDto channel) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnChannelUpdated(serverId, channel);

    public Task DispatchChannelDeletedAsync(Guid serverId, Guid channelId) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnChannelDeleted(serverId, channelId);

    public Task DispatchServerRoleCreatedAsync(Guid serverId, ServerRoleDto role) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnRoleCreated(serverId, role);

    public Task DispatchServerRoleUpdatedAsync(Guid serverId, ServerRoleDto role) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnRoleUpdated(serverId, role);

    public Task DispatchServerRoleDeletedAsync(Guid serverId, Guid roleId) =>
        hubContext.Clients
            .Group($"server_{serverId}")
            .OnRoleDeleted(serverId, roleId);

    public Task DispatchUserUpdatedAsync(Guid userId, UserDto user) =>
        DispatchToUserAsync(userId, client => client.OnUserUpdated(userId, user));

    public Task DispatchUserActivityChangedAsync(Guid userId, string activity) =>
        DispatchToUserAsync(userId, client => client.OnUserActivityChanged(userId, activity));

    public Task DispatchUserStreamingChangedAsync(Guid userId, bool isStreaming) =>
        DispatchToUserAsync(userId, client => client.OnUserStreamingChanged(userId, isStreaming));

    public Task DispatchMessagePinnedAsync(Guid channelId, MessageDto message) =>
        hubContext.Clients
            .Group($"channel_{channelId}")
            .OnMessagePinned(channelId, message);

    public Task DispatchMessageUnpinnedAsync(Guid channelId, Guid messageId) =>
        hubContext.Clients
            .Group($"channel_{channelId}")
            .OnMessageUnpinned(channelId, messageId);

    public Task DispatchMessageReplyAddedAsync(Guid messageId, MessageDto reply) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnMessageReplied(messageId, reply);

    public Task DispatchMessageReplyCountUpdatedAsync(Guid messageId, int replyCount) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnReplyCountUpdated(messageId, replyCount);

    public Task DispatchMessageReactionCountUpdatedAsync(Guid messageId, int reactionCount) =>
        hubContext.Clients
            .Group($"message_{messageId}")
            .OnReactionCountUpdated(messageId, reactionCount);

    private async Task DispatchToUserAsync(Guid userId, Func<IChatHubClient, Task> dispatch)
    {
        try
        {
            var connections = await connectionTracker.GetConnectionsAsync(userId);
            if (connections.Any())
            {
                await dispatch(hubContext.Clients.Clients(connections));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch event to user {UserId}", userId);
        }
    }
}
