using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Services.Presence;
using Chatty.Backend.Services.Servers;
using Chatty.Backend.Services.Voice;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Notifications;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Events;
using Chatty.Shared.Realtime.Hubs;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Realtime.Hubs;

[Authorize]
public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IPresenceService presenceService,
    IVoiceService voiceService,
    IConnectionTracker connectionTracker,
    IMessageService messageService,
    ITypingTracker typingTracker,
    IEventBus eventBus,
    IChannelService channelService,
    IServerService serverService,
    ChattyDbContext context,
    IValidator<NotificationPreferences> notificationSettingsValidator)
    : Hub<IChatHubClient>
{
    public async override Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            await connectionTracker.AddConnectionAsync(userId, Context.ConnectionId);
            await presenceService.UpdateLastSeenAsync(userId);
            await eventBus.PublishAsync(new PresenceEvent(userId, UserStatus.Online, null));

            logger.LogInformation("User {UserId} connected with connection {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in OnConnectedAsync");
            throw new HubException(HubError.ConnectionError("Failed to establish connection").Message);
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            await connectionTracker.RemoveConnectionAsync(userId, Context.ConnectionId);
            await presenceService.UpdateLastSeenAsync(userId);

            if (!await connectionTracker.IsOnlineAsync(userId))
            {
                await eventBus.PublishAsync(new PresenceEvent(userId, UserStatus.Offline, null));
            }

            logger.LogInformation("User {UserId} disconnected from {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in OnDisconnectedAsync");
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
                logger.LogWarning("Hub method {Method} failed: {Error}",
                    methodName, result.Error.Message);
                throw new HubException(result.Error.Message);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Serialization error in hub method {Method}", methodName);
            throw new HubException("Invalid message format");
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing hub method {Method}", methodName);
            throw new HubException(HubError.Internal($"Failed to execute {methodName}").Message);
        }
    }

    public async Task JoinChannelAsync(Guid channelId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate channel access
            var channel = await channelService.GetAsync(channelId);
            if (channel.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Channel not found"));
            }

            var canAccess = await channelService.CanAccessAsync(userId, channelId);
            if (canAccess.IsFailure || !canAccess.Value)
            {
                return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            return Result<bool>.Success(true);
        });

    public async Task StartTypingAsync(Guid channelId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate and rate limit
            var canAccess = await channelService.CanAccessAsync(userId, channelId);
            if (canAccess.IsFailure || !canAccess.Value)
            {
                return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
            }

            if (await typingTracker.IsRateLimitedAsync(userId))
            {
                return Result<bool>.Failure(Error.TooManyRequests("Too many typing indicators"));
            }

            await typingTracker.TrackTypingAsync(channelId, userId);
            var userDto = await GetUserDtoAsync(userId);
            await eventBus.PublishAsync(new TypingEvent(channelId, userDto, true));

            return Result<bool>.Success(true);
        });

    public async Task LeaveChannelAsync(Guid channelId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate channel access
            var channel = await channelService.GetAsync(channelId);
            if (channel.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Channel not found"));
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            return Result<bool>.Success(true);
        });

    public async Task StopTypingAsync(Guid channelId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var userDto = await GetUserDtoAsync(userId);
            await eventBus.PublishAsync(new TypingEvent(channelId, userDto, false));
            return Result<bool>.Success(true);
        });

    public async Task StartDirectTypingAsync(Guid recipientId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            if (await typingTracker.IsRateLimitedAsync(userId))
            {
                return Result<bool>.Failure(Error.TooManyRequests("Too many typing indicators"));
            }

            await typingTracker.TrackDirectTypingAsync(userId, recipientId);
            var userDto = await GetUserDtoAsync(userId);
            await eventBus.PublishAsync(new DirectTypingEvent(recipientId, userDto, true));

            return Result<bool>.Success(true);
        });

    public async Task StopDirectTypingAsync(Guid recipientId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var userDto = await GetUserDtoAsync(userId);
            await eventBus.PublishAsync(new DirectTypingEvent(recipientId, userDto, false));
            return Result<bool>.Success(true);
        });

    public async Task UpdatePresenceAsync(UserStatus status, string? statusMessage = null) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await presenceService.UpdateStatusAsync(userId, status, statusMessage);
            return result;
        });

    public async Task GetUserStatusAsync(Guid userId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var result = await presenceService.GetUserStatusAsync(userId);
            return result;
        });

    public async Task GetUsersStatusAsync(IEnumerable<Guid> userIds) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var result = await presenceService.GetUsersStatusAsync(userIds);
            return result;
        });

    public async Task JoinCallAsync(Guid callId, bool withVideo) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await voiceService.JoinCallAsync(callId, userId, withVideo);

            if (result.IsSuccess)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{callId}");
            }

            return result;
        });

    public async Task LeaveCallAsync(Guid callId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await voiceService.LeaveCallAsync(callId, userId);

            if (result.IsSuccess)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call_{callId}");
            }

            return result;
        });

    public async Task MuteAsync(Guid callId, bool muted) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            return await voiceService.MuteParticipantAsync(callId, userId, muted);
        });

    public async Task EnableVideoAsync(Guid callId, bool enabled) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            return await voiceService.EnableVideoAsync(callId, userId, enabled);
        });

    public async Task SendSignalingMessageAsync(Guid callId, Guid peerId, string type, string data) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate that both users are in the call
            var participants = await voiceService.GetParticipantsAsync(callId);
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
            var connections = await connectionTracker.GetConnectionsAsync(peerId);
            if (connections.Count > 0)
            {
                await Clients.Clients(connections)
                    .OnSignalingMessage(userId, type, data);
            }

            return Result<bool>.Success(true);
        });

    // Server member operations
    public async Task JoinServerAsync(Guid serverId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Verify server exists
            var server = await serverService.GetAsync(serverId);
            if (server.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Server not found"));
            }

            // Check if already a member
            var isMember = await context.ServerMembers
                .AnyAsync(m => m.ServerId == serverId && m.UserId == userId);
            if (isMember)
            {
                return Result<bool>.Failure(Error.Conflict("Already a member of this server"));
            }

            var result = await serverService.AddMemberAsync(serverId, userId);
            if (result.IsSuccess)
            {
                // Add to server group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"server_{serverId}");

                // Get member details from server
                var memberResult = await context.ServerMembers
                    .Include(m => m.User)
                    .Include(m => m.Role)
                    .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId);

                if (memberResult != null)
                {
                    // Publish member joined event
                    await eventBus.PublishAsync(new ServerMemberJoinedEvent(serverId, memberResult.ToDto()));
                }

                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task LeaveServerAsync(Guid serverId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Verify server exists and user is a member
            var server = await serverService.GetAsync(serverId);
            if (server.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Server not found"));
            }

            var isMember = await context.ServerMembers
                .AnyAsync(m => m.ServerId == serverId && m.UserId == userId);
            if (!isMember)
            {
                return Result<bool>.Failure(Error.NotFound("Not a member of this server"));
            }

            var result = await serverService.RemoveMemberAsync(serverId, userId);
            if (result.IsSuccess)
            {
                // Remove from server group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server_{serverId}");

                // Remove from all server's channel groups
                var channels = await channelService.GetForServerAsync(serverId);
                if (channels.IsSuccess)
                {
                    foreach (var channel in channels.Value)
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channel.Id}");
                    }
                }

                // Publish member left event
                var userDto = await GetUserDtoAsync(userId);
                await eventBus.PublishAsync(new ServerMemberRemovedEvent(serverId, userDto));

                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task UpdateMemberRoleAsync(Guid serverId, Guid userId, Guid roleId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var currentUserId = GetUserId();

            // Verify server exists
            var server = await serverService.GetAsync(serverId);
            if (server.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Server not found"));
            }

            // Verify role exists
            var role = await context.ServerRoles
                .FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);
            if (role == null)
            {
                return Result<bool>.Failure(Error.NotFound("Role not found"));
            }

            // Verify current user has manage roles permission
            var currentMember = await context.ServerMembers
                .Include(m => m.Role)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == currentUserId);

            if (currentMember?.Role?.Permissions.Any(p => p.Permission == PermissionType.ManageRoles) != true)
            {
                return Result<bool>.Failure(Error.Forbidden("Missing required permission: ManageRoles"));
            }

            var result = await serverService.UpdateMemberRoleAsync(serverId, userId, roleId);
            if (result.IsSuccess)
            {
                // Get updated member details
                var member = await context.ServerMembers
                    .Include(m => m.User)
                    .Include(m => m.Role)
                    .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId);

                if (member != null)
                {
                    // Publish member updated event
                    await eventBus.PublishAsync(new ServerMemberUpdatedEvent(serverId, member.ToDto()));
                }

                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task KickMemberAsync(Guid serverId, Guid userId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var currentUserId = GetUserId();

            // Verify server exists
            var server = await serverService.GetAsync(serverId);
            if (server.IsFailure)
            {
                return Result<bool>.Failure(Error.NotFound("Server not found"));
            }

            // Cannot kick yourself
            if (userId == currentUserId)
            {
                return Result<bool>.Failure(Error.Validation("Cannot kick yourself"));
            }

            // Verify target user is a member
            var isMember = await context.ServerMembers
                .AnyAsync(m => m.ServerId == serverId && m.UserId == userId);
            if (!isMember)
            {
                return Result<bool>.Failure(Error.NotFound("User is not a member of this server"));
            }

            // Verify current user has kick members permission
            var currentMember = await context.ServerMembers
                .Include(m => m.Role)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == currentUserId);

            if (currentMember?.Role?.Permissions.Any(p => p.Permission == PermissionType.KickMembers) != true)
            {
                return Result<bool>.Failure(Error.Forbidden("Missing required permission: KickMembers"));
            }

            var result = await serverService.RemoveMemberAsync(serverId, userId);
            if (result.IsSuccess)
            {
                // Publish member removed event
                var userDto = await GetUserDtoAsync(userId);
                await eventBus.PublishAsync(new ServerMemberRemovedEvent(serverId, userDto));

                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    // Notification operations
    public async Task SubscribeToNotificationsAsync(string deviceToken, DeviceType deviceType) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(deviceToken))
            {
                return Result<bool>.Failure(Error.Validation("Device token is required"));
            }

            // Register or update device
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken);

            if (device == null)
            {
                device = new UserDevice
                {
                    UserId = userId,
                    DeviceId = Guid.NewGuid(),
                    DeviceToken = deviceToken,
                    DeviceType = deviceType,
                    PublicKey = [], // Should be set by client
                    CreatedAt = DateTime.UtcNow
                };
                context.UserDevices.Add(device);
            }
            else if (device.UserId != userId)
            {
                return Result<bool>.Failure(Error.Forbidden("Device token belongs to another user"));
            }
            else
            {
                device.DeviceType = deviceType;
                device.LastActiveAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        });

    public async Task UnsubscribeFromNotificationsAsync(string deviceToken) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(deviceToken))
            {
                return Result<bool>.Failure(Error.Validation("Device token is required"));
            }

            // Remove device registration
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken);

            if (device != null)
            {
                if (device.UserId != userId)
                {
                    return Result<bool>.Failure(Error.Forbidden("Device token belongs to another user"));
                }

                context.UserDevices.Remove(device);
                await context.SaveChangesAsync();
            }

            return Result<bool>.Success(true);
        });

    public async Task UpdateNotificationPreferencesAsync(NotificationPreferences preferences) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();

            // Validate settings
            var validationResult = await notificationSettingsValidator.ValidateAsync(preferences);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(Error.Validation(
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            // Update user notification settings
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure(Error.NotFound("User not found"));
            }

            user.NotificationPreferences = preferences;
            user.LastOnlineAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        });

    public async Task SendMessageAsync(CreateMessageRequest request) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.CreateAsync(userId, request);
            if (result.IsSuccess)
            {
                // The message event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task UpdateMessageAsync(Guid messageId, UpdateMessageRequest request) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.UpdateAsync(messageId, userId, request);
            if (result.IsSuccess)
            {
                // The message update event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task DeleteMessageAsync(Guid messageId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.DeleteAsync(messageId, userId);
            if (result.IsSuccess)
            {
                // The message delete event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task SendDirectMessageAsync(CreateDirectMessageRequest request) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.CreateDirectAsync(userId, request);
            if (result.IsSuccess)
            {
                // The direct message event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task UpdateDirectMessageAsync(Guid messageId, UpdateDirectMessageRequest request) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.UpdateDirectAsync(messageId, userId, request);
            if (result.IsSuccess)
            {
                // The direct message update event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task DeleteDirectMessageAsync(Guid messageId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.DeleteDirectAsync(messageId, userId);
            if (result.IsSuccess)
            {
                // The direct message delete event will be published through the event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    // Message reply operations
    public async Task ReplyToMessageAsync(Guid messageId, ReplyMessageRequest request) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.ReplyAsync(messageId, userId, request);
            if (result.IsSuccess)
            {
                // Reply event will be published through event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    // Pin operations
    public async Task PinMessageAsync(Guid channelId, Guid messageId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.PinMessageAsync(channelId, messageId, userId);
            if (result.IsSuccess)
            {
                // Pin event will be published through event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    public async Task UnpinMessageAsync(Guid channelId, Guid messageId) =>
        await ExecuteHubMethodAsync(async () =>
        {
            var userId = GetUserId();
            var result = await messageService.UnpinMessageAsync(channelId, messageId, userId);
            if (result.IsSuccess)
            {
                // Unpin event will be published through event bus
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(result.Error);
        });

    private Guid GetUserId()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            logger.LogWarning("Unauthorized hub access attempt");
            throw new HubException(HubError.Unauthorized("User not authenticated").Message);
        }

        return Guid.Parse(userId);
    }

    private async Task<UserDto> GetUserDtoAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user?.ToDto() ?? throw new HubException(HubError.NotFound("User not found").Message);
    }
}
