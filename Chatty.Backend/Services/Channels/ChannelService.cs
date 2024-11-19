using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chatty.Backend.Services.Channels;

public sealed class ChannelService(
    ChattyDbContext context,
    IEventBus eventBus,
    ILogger<ChannelService> logger,
    IOptions<LimitSettings> limitSettings)
    : IChannelService
{
    private readonly LimitSettings _limitSettings = limitSettings.Value;

    public async Task<Result<ChannelDto>> CreateAsync(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken ct = default)
    {
        var server = await context.Servers
            .Include(s => s.Channels)
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
        {
            return Result<ChannelDto>.Failure(Error.NotFound("Server not found"));
        }

        // Check channel limit
        if (server.Channels.Count >= _limitSettings.MaxChannelsPerServer)
        {
            return Result<ChannelDto>.Failure(
                Error.Validation($"Server cannot have more than {_limitSettings.MaxChannelsPerServer} channels"));
        }

        // Validate rate limit if specified
        if (request.RateLimitPerUser > 0)
        {
            var maxRateLimit = _limitSettings.RateLimits.Messages.DurationSeconds;
            if (request.RateLimitPerUser > maxRateLimit)
            {
                return Result<ChannelDto>.Failure(Error.Validation($"Rate limit cannot exceed {maxRateLimit} seconds"));
            }
        }

        try
        {
            var channel = new Channel
            {
                ServerId = serverId,
                Name = request.Name,
                Topic = request.Topic,
                IsPrivate = request.IsPrivate,
                ChannelType = request.ChannelType,
                Position = request.Position,
                RateLimitPerUser = request.RateLimitPerUser
            };

            context.Channels.Add(channel);
            await context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await context.Entry(channel)
                .Reference(c => c.Server)
                .LoadAsync(ct);

            await context.Entry(channel)
                .Collection(c => c.Members)
                .LoadAsync(ct);

            var channelDto = channel.ToDto();

            // Publish channel created event
            await eventBus.PublishAsync(
                new ChannelCreatedEvent(serverId, channelDto),
                ct);

            return Result<ChannelDto>.Success(channelDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create channel in server {ServerId}", serverId);
            return Result<ChannelDto>.Failure(Error.Internal("Failed to create channel"));
        }
    }

    public async Task<Result<ChannelDto>> UpdateAsync(
        Guid channelId,
        UpdateChannelRequest request,
        CancellationToken ct = default)
    {
        var channel = await context.Channels.FindAsync([channelId], ct);
        if (channel is null)
        {
            return Result<ChannelDto>.Failure(Error.NotFound("Channel not found"));
        }

        try
        {
            // Validate rate limit if being updated
            if (request.RateLimitPerUser is > 0)
            {
                var maxRateLimit = _limitSettings.RateLimits.Messages.DurationSeconds;
                if (request.RateLimitPerUser.Value > maxRateLimit)
                {
                    return Result<ChannelDto>.Failure(
                        Error.Validation($"Rate limit cannot exceed {maxRateLimit} seconds"));
                }
            }

            if (request.Name is not null)
            {
                channel.Name = request.Name;
            }

            if (request.Topic is not null)
            {
                channel.Topic = request.Topic;
            }

            if (request.Position.HasValue)
            {
                channel.Position = request.Position.Value;
            }

            if (request.RateLimitPerUser.HasValue)
            {
                channel.RateLimitPerUser = request.RateLimitPerUser.Value;
            }

            channel.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await context.Entry(channel)
                .Reference(c => c.Server)
                .LoadAsync(ct);

            await context.Entry(channel)
                .Collection(c => c.Members)
                .LoadAsync(ct);

            var channelDto = channel.ToDto();

            // Publish channel updated event
            await eventBus.PublishAsync(
                new ChannelUpdatedEvent(channel.ServerId, channelDto),
                ct);

            return Result<ChannelDto>.Success(channelDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update channel {ChannelId}", channelId);
            return Result<ChannelDto>.Failure(Error.Internal("Failed to update channel"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid channelId,
        CancellationToken ct = default)
    {
        var channel = await context.Channels.FindAsync([channelId], ct);
        if (channel is null)
        {
            return Result<bool>.Success(true); // Already deleted
        }

        try
        {
            var serverId = channel.ServerId;
            context.Channels.Remove(channel);
            await context.SaveChangesAsync(ct);

            // Publish channel deleted event
            await eventBus.PublishAsync(
                new ChannelDeletedEvent(serverId, channelId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete channel {ChannelId}", channelId);
            return Result<bool>.Failure(Error.Internal("Failed to delete channel"));
        }
    }

    public async Task<Result<ChannelDto>> GetAsync(
        Guid channelId,
        CancellationToken ct = default)
    {
        var channel = await context.Channels
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == channelId, ct);

        if (channel is null)
        {
            return Result<ChannelDto>.Failure(Error.NotFound("Channel not found"));
        }

        // Load relationships for DTO
        await context.Entry(channel)
            .Reference(c => c.Server)
            .LoadAsync(ct);

        var channelDto = channel.ToDto();

        return Result<ChannelDto>.Success(channelDto);
    }

    public async Task<Result<IReadOnlyList<ChannelDto>>> GetForServerAsync(
        Guid serverId,
        CancellationToken ct = default)
    {
        var channels = await context.Channels
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .Where(c => c.ServerId == serverId)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        foreach (var channel in channels)
        {
            // Load relationships for DTO
            await context.Entry(channel)
                .Reference(c => c.Server)
                .LoadAsync(ct);
        }

        return Result<IReadOnlyList<ChannelDto>>.Success(
            channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<bool>> AddMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken ct = default)
    {
        var channel = await context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == channelId, ct);

        if (channel is null)
        {
            return Result<bool>.Failure(Error.NotFound("Channel not found"));
        }

        if (channel.Members.Any(m => m.UserId == userId))
        {
            return Result<bool>.Success(true); // Already a member
        }

        try
        {
            var member = new ChannelMember
            {
                ChannelId = channelId,
                UserId = userId
            };

            context.ChannelMembers.Add(member);
            await context.SaveChangesAsync(ct);

            // Load user for event
            var user = await context.Users.FindAsync([userId], ct);

            // Publish member joined event
            await eventBus.PublishAsync(
                new ChannelMemberJoinedEvent(channel.ServerId, channelId, user!.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add member {UserId} to channel {ChannelId}", userId, channelId);
            return Result<bool>.Failure(Error.Internal("Failed to add member"));
        }
    }

    public async Task<Result<bool>> RemoveMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken ct = default)
    {
        var member = await context.ChannelMembers
            .Include(m => m.Channel)
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId, ct);

        if (member is null)
        {
            return Result<bool>.Success(true); // Already removed
        }

        try
        {
            context.ChannelMembers.Remove(member);
            await context.SaveChangesAsync(ct);

            // Load user for event
            var user = await context.Users.FindAsync([userId], ct);

            // Publish member left event
            await eventBus.PublishAsync(
                new ChannelMemberLeftEvent(member.Channel.ServerId, channelId, user!.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove member {UserId} from channel {ChannelId}", userId, channelId);
            return Result<bool>.Failure(Error.Internal("Failed to remove member"));
        }
    }

    public async Task<Result<bool>> CanAccessAsync(
        Guid userId,
        Guid channelId,
        CancellationToken ct = default)
    {
        try
        {
            var channel = await context.Channels
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == channelId, ct);

            if (channel is null)
            {
                return Result<bool>.Failure(Error.NotFound("Channel not found"));
            }

            // Load relationships for DTO
            await context.Entry(channel)
                .Reference(c => c.Server)
                .LoadAsync(ct);

            // If channel is not private, anyone can access
            if (!channel.IsPrivate)
            {
                return Result<bool>.Success(true);
            }

            // For private channels, check membership
            var isMember = channel.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                return Result<bool>.Failure(Error.Forbidden("User is not a member of this channel"));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check channel access for user {UserId} in channel {ChannelId}",
                userId, channelId);
            return Result<bool>.Failure(Error.Internal("Failed to check channel access"));
        }
    }

    public async Task<Result<bool>> HasPermissionAsync(
        Guid userId,
        Guid channelId,
        PermissionType permission,
        CancellationToken ct = default)
    {
        var channel = await context.Channels
            .Include(c => c.Server!)
            .ThenInclude(s => s.Members)
            .ThenInclude(m => m.Role!)
            .ThenInclude(serverRole => serverRole.Permissions)
            .FirstOrDefaultAsync(c => c.Id == channelId, ct);

        if (channel is null)
        {
            return Result<bool>.Failure(Error.NotFound("Channel not found"));
        }

        // Server owner has all permissions
        if (channel.Server?.OwnerId == userId)
        {
            return Result<bool>.Success(true);
        }

        var member = channel.Server?.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return Result<bool>.Failure(Error.Forbidden("User is not a member of this server"));
        }

        // Check if the user's role has the required permission
        if (member.Role?.Permissions != null && member.Role.Permissions.Any(p => p.Permission == permission))
        {
            return Result<bool>.Success(true);
        }

        return Result<bool>.Success(false);
    }
}
