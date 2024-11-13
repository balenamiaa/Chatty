using Chatty.Backend.Data;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using IChatHubClient = Chatty.Backend.Realtime.Hubs.IChatHubClient;

namespace Chatty.Backend.Realtime.Events;

public sealed class EventDispatcher : IEventDispatcher
{
    private readonly IHubContext<ChatHub, IChatHubClient> _hubContext;
    private readonly IConnectionTracker _connectionTracker;
    private readonly ILogger<EventDispatcher> _logger;
    private readonly ChattyDbContext _context;

    public EventDispatcher(
        IHubContext<ChatHub, IChatHubClient> hubContext,
        IConnectionTracker connectionTracker,
        ILogger<EventDispatcher> logger,
        ChattyDbContext context)
    {
        _hubContext = hubContext;
        _connectionTracker = connectionTracker;
        _logger = logger;
        _context = context;
    }

    public async Task DispatchMessageReceivedAsync(Guid channelId, MessageDto message)
    {
        try
        {
            // Broadcast to channel group
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageReceived(channelId, message);

            // Also notify channel members individually for their notifications
            var channelMembers = await _context.ChannelMembers
                .Where(m => m.ChannelId == channelId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in channelMembers)
            {
                var connections = await _connectionTracker.GetConnectionsAsync(memberId);
                if (connections.Count > 0)
                {
                    await _hubContext.Clients
                        .Clients(connections)
                        .OnNotification(
                            "New Message",
                            $"New message in channel from {message.Sender.Username}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch message received event");
        }
    }

    public async Task DispatchMessageUpdatedAsync(Guid channelId, MessageDto message)
    {
        try
        {
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageUpdated(channelId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch message updated event");
        }
    }

    public async Task DispatchMessageDeletedAsync(Guid channelId, Guid messageId)
    {
        try
        {
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .OnMessageDeleted(channelId, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch message deleted event");
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
            _logger.LogError(ex, "Failed to dispatch direct message received event");
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
            _logger.LogError(ex, "Failed to dispatch direct message updated event");
        }
    }

    public async Task DispatchDirectMessageDeletedAsync(Guid messageId)
    {
        try
        {
            // Note: This needs to be dispatched to both sender and recipient
            // You might want to modify the method signature to include both IDs
            await _hubContext.Clients.All.OnDirectMessageDeleted(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch direct message deleted event");
        }
    }

    public async Task DispatchTypingStartedAsync(Guid channelId, UserDto user)
    {
        try
        {
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .OnTypingStarted(channelId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch typing started event");
        }
    }

    public async Task DispatchTypingStoppedAsync(Guid channelId, UserDto user)
    {
        try
        {
            await _hubContext.Clients
                .Group($"channel_{channelId}")
                .OnTypingStopped(channelId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch typing stopped event");
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
            _logger.LogError(ex, "Failed to dispatch direct typing started event");
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
            _logger.LogError(ex, "Failed to dispatch direct typing stopped event");
        }
    }

    public async Task DispatchUserPresenceChangedAsync(Guid userId, UserStatus status, string? statusMessage)
    {
        try
        {
            await _hubContext.Clients.All
                .OnUserPresenceChanged(userId, status, statusMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch user presence changed event");
        }
    }

    public async Task DispatchUserOnlineStateChangedAsync(Guid userId, bool isOnline)
    {
        try
        {
            await _hubContext.Clients.All
                .OnUserOnlineStateChanged(userId, isOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch user online state changed event");
        }
    }

    public async Task DispatchCallStartedAsync(CallDto call)
    {
        try
        {
            if (call.ChannelId.HasValue)
            {
                await _hubContext.Clients
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
            _logger.LogError(ex, "Failed to dispatch call started event");
        }
    }

    public async Task DispatchCallEndedAsync(Guid callId)
    {
        try
        {
            await _hubContext.Clients.All.OnCallEnded(callId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch call ended event");
        }
    }

    public async Task DispatchParticipantJoinedAsync(Guid callId, CallParticipantDto participant)
    {
        try
        {
            await _hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantJoined(callId, participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch participant joined event");
        }
    }

    public async Task DispatchParticipantLeftAsync(Guid callId, Guid userId)
    {
        try
        {
            await _hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantLeft(callId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch participant left event");
        }
    }

    public async Task DispatchParticipantMutedAsync(Guid callId, Guid userId, bool isMuted)
    {
        try
        {
            await _hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantMuted(callId, userId, isMuted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch participant muted event");
        }
    }

    public async Task DispatchParticipantVideoEnabledAsync(Guid callId, Guid userId, bool isEnabled)
    {
        try
        {
            await _hubContext.Clients
                .Group($"call_{callId}")
                .OnParticipantVideoEnabled(callId, userId, isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch participant video enabled event");
        }
    }

    private async Task DispatchToUserAsync(Guid userId, Func<IChatHubClient, Task> dispatch)
    {
        try
        {
            var connections = await _connectionTracker.GetConnectionsAsync(userId);
            if (connections.Count > 0)
            {
                await dispatch(_hubContext.Clients.Clients(connections));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch event to user {UserId}", userId);
        }
    }
}