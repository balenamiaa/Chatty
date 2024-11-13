using System.Security.Claims;
using Chatty.Backend.Realtime.Events;
using Microsoft.AspNetCore.SignalR;
using Chatty.Shared.Realtime.Hubs;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Services.Voice;
using Chatty.Backend.Services.Presence;
using Chatty.Backend.Services.Users;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Events;

namespace Chatty.Backend.Realtime.Hubs;

public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IPresenceService presenceService,
    IVoiceService voiceService,
    IConnectionTracker connectionTracker,
    IMessageService messageService,
    ITypingTracker typingTracker,
    IEventBus eventBus)
    : Hub<IChatHubClient>, IChatHub
{
    private readonly ILogger<ChatHub> _logger = logger;
    private readonly IMessageService _messageService = messageService;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await connectionTracker.AddConnectionAsync(userId, Context.ConnectionId);
        await presenceService.UpdateLastSeenAsync(userId);
        await eventBus.PublishAsync(new OnlineStateEvent(userId, true));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await connectionTracker.RemoveConnectionAsync(userId, Context.ConnectionId);
        await presenceService.UpdateLastSeenAsync(userId);

        if (!await connectionTracker.IsOnlineAsync(userId))
        {
            await eventBus.PublishAsync(new OnlineStateEvent(userId, false));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
    }

    public async Task StartTypingAsync(Guid channelId)
    {
        var userId = GetUserId();
        await typingTracker.TrackTypingAsync(channelId, userId);

        var user = await GetUserDtoAsync(userId);
        await eventBus.PublishAsync(new TypingEvent(channelId, user, true));
    }

    public async Task StopTypingAsync(Guid channelId)
    {
        var userId = GetUserId();
        var user = await GetUserDtoAsync(userId);
        await eventBus.PublishAsync(new TypingEvent(channelId, user, false));
    }

    public async Task StartDirectTypingAsync(Guid recipientId)
    {
        var userId = GetUserId();
        await typingTracker.TrackDirectTypingAsync(userId, recipientId);

        var user = await GetUserDtoAsync(userId);
        await eventBus.PublishAsync(new DirectTypingEvent(recipientId, user, true));
    }

    public async Task StopDirectTypingAsync(Guid recipientId)
    {
        var userId = GetUserId();
        var user = await GetUserDtoAsync(userId);
        await eventBus.PublishAsync(new DirectTypingEvent(recipientId, user, false));
    }

    public async Task UpdatePresenceAsync(UserStatus status, string? statusMessage = null)
    {
        var userId = GetUserId();
        await presenceService.UpdateStatusAsync(userId, status, statusMessage);
    }

    public async Task JoinCallAsync(Guid callId, bool withVideo)
    {
        var userId = GetUserId();
        await voiceService.JoinCallAsync(callId, userId, withVideo);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{callId}");
    }

    public async Task LeaveCallAsync(Guid callId)
    {
        var userId = GetUserId();
        await voiceService.LeaveCallAsync(callId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call_{callId}");
    }

    public async Task MuteAsync(Guid callId, bool muted)
    {
        var userId = GetUserId();
        await voiceService.MuteParticipantAsync(callId, userId, muted);
    }

    public async Task EnableVideoAsync(Guid callId, bool enabled)
    {
        var userId = GetUserId();
        await voiceService.EnableVideoAsync(callId, userId, enabled);
    }

    public async Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data)
    {
        var userId = GetUserId();
        var connections = await connectionTracker.GetConnectionsAsync(peerId);

        if (connections.Count > 0)
        {
            await Clients.Clients(connections)
                .OnSignalingMessage(callId, userId, type, data);
        }
    }

    private Guid GetUserId()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            throw new HubException("Unauthorized");
        }

        return Guid.Parse(userId);
    }

    private async Task<UserDto> GetUserDtoAsync(Guid userId)
    {
        var userService = Context.GetHttpContext()?.RequestServices.GetRequiredService<IUserService>();
        if (userService is null)
            throw new HubException("Could not resolve user service");

        var result = await userService.GetByIdAsync(userId);
        if (result.IsFailure)
            throw new HubException($"Could not get user: {result.Error.Message}");

        return result.Value;
    }
}