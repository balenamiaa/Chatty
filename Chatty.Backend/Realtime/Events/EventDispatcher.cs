using Chatty.Backend.Data;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Shared.Models.Calls;
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
    IConnectionTracker connectionTracker,
    ILogger<EventDispatcher> logger,
    ChattyDbContext context)
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
                if (connections.Count > 0)
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

    public async Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage)
    {
        try
        {
            await hubContext.Clients.All
                .OnUserPresenceChanged(userId, status, statusMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch user presence changed event");
        }
    }

    public async Task DispatchUserOnlineStateChangedAsync(Guid userId, bool isOnline)
    {
        try
        {
            await hubContext.Clients.All
                .OnUserOnlineStateChanged(userId, isOnline);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch user online state changed event");
        }
    }

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

    public async Task DispatchServerMemberJoinedAsync(Guid serverId, ServerMemberDto member)
    {
        try
        {
            await hubContext.Clients
                .Group($"server_{serverId}")
                .OnMemberJoined(serverId, member);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch server member joined event");
        }
    }

    public async Task DispatchServerMemberLeftAsync(Guid serverId, Guid userId)
    {
        try
        {
            await hubContext.Clients
                .Group($"server_{serverId}")
                .OnMemberLeft(serverId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch server member left event");
        }
    }

    public async Task DispatchServerMemberUpdatedAsync(Guid serverId, ServerMemberDto member)
    {
        try
        {
            await hubContext.Clients
                .Group($"server_{serverId}")
                .OnMemberUpdated(serverId, member);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch server member updated event");
        }
    }

    public async Task DispatchServerMemberKickedAsync(Guid serverId, Guid userId)
    {
        try
        {
            // Notify server members
            await hubContext.Clients
                .Group($"server_{serverId}")
                .OnMemberLeft(serverId, userId);

            // Notify kicked user
            var connections = await connectionTracker.GetConnectionsAsync(userId);
            if (connections.Count > 0)
            {
                await hubContext.Clients
                    .Clients(connections)
                    .OnNotification("Server Kick", "You have been removed from the server");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch server member kicked event");
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

    public async Task DispatchMessageReactionAddedAsync(Guid channelId, Guid messageId, MessageReactionDto reaction)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageReactionAdded(channelId, messageId, reaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch message reaction added event");
        }
    }

    public async Task DispatchMessageReactionRemovedAsync(Guid channelId, Guid messageId, Guid reactionId, Guid userId)
    {
        try
        {
            await hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageReactionRemoved(channelId, messageId, reactionId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch message reaction removed event");
        }
    }

    public async Task DispatchDirectMessageReactionAddedAsync(Guid messageId, MessageReactionDto reaction)
    {
        try
        {
            var directMessage = await context.DirectMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (directMessage is not null)
            {
                await DispatchToUserAsync(directMessage.SenderId, client =>
                    client.OnDirectMessageReactionAdded(messageId, reaction));

                await DispatchToUserAsync(directMessage.RecipientId, client =>
                    client.OnDirectMessageReactionAdded(messageId, reaction));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct message reaction added event");
        }
    }

    public async Task DispatchDirectMessageReactionRemovedAsync(Guid messageId, Guid reactionId, Guid userId)
    {
        try
        {
            var directMessage = await context.DirectMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (directMessage is not null)
            {
                await DispatchToUserAsync(directMessage.SenderId, client =>
                    client.OnDirectMessageReactionRemoved(messageId, reactionId, userId));

                await DispatchToUserAsync(directMessage.RecipientId, client =>
                    client.OnDirectMessageReactionRemoved(messageId, reactionId, userId));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch direct message reaction removed event");
        }
    }

    private async Task DispatchToUserAsync(Guid userId, Func<IChatHubClient, Task> dispatch)
    {
        try
        {
            var connections = await connectionTracker.GetConnectionsAsync(userId);
            if (connections.Count > 0)
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
