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
using Chatty.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.CompilerServices;
using Chatty.Backend.Services.Channels;

namespace Chatty.Backend.Realtime.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatHubClient>, IChatHub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IPresenceService _presenceService;
    private readonly IVoiceService _voiceService;
    private readonly IConnectionTracker _connectionTracker;
    private readonly IMessageService _messageService;
    private readonly ITypingTracker _typingTracker;
    private readonly IEventBus _eventBus;
    private readonly IChannelService _channelService;

    public ChatHub(
        ILogger<ChatHub> logger,
        IPresenceService presenceService,
        IVoiceService voiceService,
        IConnectionTracker connectionTracker,
        IMessageService messageService,
        ITypingTracker typingTracker,
        IEventBus eventBus,
        IChannelService channelService)
    {
        _logger = logger;
        _presenceService = presenceService;
        _voiceService = voiceService;
        _connectionTracker = connectionTracker;
        _messageService = messageService;
        _typingTracker = typingTracker;
        _eventBus = eventBus;
        _channelService = channelService;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            await _connectionTracker.AddConnectionAsync(userId, Context.ConnectionId);
            await _presenceService.UpdateLastSeenAsync(userId);
            await _eventBus.PublishAsync(new OnlineStateEvent(userId, true));

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
            throw new HubException(HubError.ConnectionError("Failed to establish connection").Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            await _connectionTracker.RemoveConnectionAsync(userId, Context.ConnectionId);
            await _presenceService.UpdateLastSeenAsync(userId);

            if (!await _connectionTracker.IsOnlineAsync(userId))
            {
                await _eventBus.PublishAsync(new OnlineStateEvent(userId, false));
            }

            _logger.LogInformation("User {UserId} disconnected from {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
            throw new HubException(HubError.ConnectionError("Failed to handle disconnection").Message);
        }
    }

    private async Task ExecuteHubMethodAsync<T>(
        Func<Task<Result<T>>> action,
        [CallerMemberName] string methodName = "")
    {
        try
        {
            var result = await action();
            if (result.IsFailure)
            {
                _logger.LogWarning("Hub method {Method} failed: {Error}",
                    methodName, result.Error.Message);
                throw new HubException(result.Error.Message);
            }
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing hub method {Method}", methodName);
            throw new HubException(HubError.Internal($"Failed to execute {methodName}").Message);
        }
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate channel access
            var channel = await _channelService.GetAsync(channelId);
            if (channel.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Channel not found"));
            }

            var canAccess = await _channelService.CanAccessAsync(userId, channelId);
            if (canAccess.IsFailure || !canAccess.Value)
            {
                return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            return Result<bool>.Success(true);
        });
    }

    public async Task StartTypingAsync(Guid channelId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate and rate limit
            var canAccess = await _channelService.CanAccessAsync(userId, channelId);
            if (canAccess.IsFailure || !canAccess.Value)
            {
                return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
            }

            if (await _typingTracker.IsRateLimitedAsync(userId))
            {
                return Result<bool>.Failure(Error.TooManyRequests("Too many typing indicators"));
            }

            await _typingTracker.TrackTypingAsync(channelId, userId);
            var user = await GetUserDtoAsync(userId);
            await _eventBus.PublishAsync(new TypingEvent(channelId, user, true));

            return Result<bool>.Success(true);
        });
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate channel access
            var channel = await _channelService.GetAsync(channelId);
            if (channel.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Channel not found"));
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            return Result<bool>.Success(true);
        });
    }

    public async Task StopTypingAsync(Guid channelId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var user = await GetUserDtoAsync(userId);
            await _eventBus.PublishAsync(new TypingEvent(channelId, user, false));
            return Result<bool>.Success(true);
        });
    }

    public async Task StartDirectTypingAsync(Guid recipientId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            if (await _typingTracker.IsRateLimitedAsync(userId))
            {
                return Result<bool>.Failure(Error.TooManyRequests("Too many typing indicators"));
            }

            await _typingTracker.TrackDirectTypingAsync(userId, recipientId);
            var user = await GetUserDtoAsync(userId);
            await _eventBus.PublishAsync(new DirectTypingEvent(recipientId, user, true));

            return Result<bool>.Success(true);
        });
    }

    public async Task StopDirectTypingAsync(Guid recipientId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var user = await GetUserDtoAsync(userId);
            await _eventBus.PublishAsync(new DirectTypingEvent(recipientId, user, false));
            return Result<bool>.Success(true);
        });
    }

    public async Task UpdatePresenceAsync(UserStatus status, string? statusMessage = null)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await _presenceService.UpdateStatusAsync(userId, status, statusMessage);
            return result;
        });
    }

    public async Task JoinCallAsync(Guid callId, bool withVideo)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await _voiceService.JoinCallAsync(callId, userId, withVideo);

            if (result.IsSuccess)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{callId}");
            }

            return result;
        });
    }

    public async Task LeaveCallAsync(Guid callId)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await _voiceService.LeaveCallAsync(callId, userId);

            if (result.IsSuccess)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call_{callId}");
            }

            return result;
        });
    }

    public async Task MuteAsync(Guid callId, bool muted)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            return await _voiceService.MuteParticipantAsync(callId, userId, muted);
        });
    }

    public async Task EnableVideoAsync(Guid callId, bool enabled)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            return await _voiceService.EnableVideoAsync(callId, userId, enabled);
        });
    }

    public async Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data)
    {
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate that both users are in the call
            var participants = await _voiceService.GetParticipantsAsync(callId);
            if (participants.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Call not found"));
            }

            var isUserInCall = participants.Value.Any(p => p.User.Id == userId);
            var isPeerInCall = participants.Value.Any(p => p.User.Id == peerId);

            if (!isUserInCall || !isPeerInCall)
            {
                return Result<bool>.Failure(Error.Forbidden("User or peer not in call"));
            }

            // Send the signaling message to the peer
            var connections = await _connectionTracker.GetConnectionsAsync(peerId);
            if (connections.Count > 0)
            {
                await Clients.Clients(connections)
                    .OnSignalingMessage(callId, userId, type, data);
            }

            return Result<bool>.Success(true);
        });
    }

    private Guid GetUserId()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            _logger.LogWarning("Unauthorized hub access attempt");
            throw new HubException(HubError.Unauthorized("User not authenticated").Message);
        }

        return Guid.Parse(userId);
    }

    private async Task<UserDto> GetUserDtoAsync(Guid userId)
    {
        var userService = Context.GetHttpContext()?.RequestServices.GetRequiredService<IUserService>();
        if (userService is null)
        {
            throw new HubException(HubError.Internal("Service unavailable").Message);
        }

        var result = await userService.GetByIdAsync(userId);
        if (result.IsFailure)
        {
            throw new HubException(HubError.NotFound($"User not found: {result.Error.Message}").Message);
        }

        return result.Value;
    }
}