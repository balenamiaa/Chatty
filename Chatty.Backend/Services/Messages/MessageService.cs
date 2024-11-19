using System.Collections.Concurrent;

using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Channels;
using Chatty.Shared.Crypto;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chatty.Backend.Services.Messages;

public sealed class MessageService(
    IDbContextFactory<ChattyDbContext> contextFactory,
    IEventBus eventBus,
    ICryptoProvider crypto,
    IChannelService channelService,
    ILogger<MessageService> logger,
    IOptions<LimitSettings> limitSettings) : IMessageService
{
    private static readonly ConcurrentDictionary<Guid, (int Count, DateTime Start)> _rateLimits = new();
    private readonly LimitSettings _limitSettings = limitSettings.Value;

    public async Task<Result<MessageDto>> CreateAsync(
        Guid userId,
        CreateMessageRequest request,
        CancellationToken ct = default)
    {
        // Validate message length
        if (request.Content.Length > _limitSettings.MaxMessageLength)
        {
            return Result<MessageDto>.Failure(
                Error.Validation($"Message exceeds maximum length of {_limitSettings.MaxMessageLength} bytes"));
        }

        // Check rate limit
        if (!await CheckRateLimitAsync(userId, ct))
        {
            return Result<MessageDto>.Failure(Error.TooManyRequests("Message rate limit exceeded"));
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Verify channel exists and user has access
        var channel = await context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
        {
            return Result<MessageDto>.Failure(Error.NotFound("Channel not found"));
        }

        if (channel.Members.All(m => m.UserId != userId))
        {
            return Result<MessageDto>.Failure(Error.Forbidden("User is not a member of this channel"));
        }

        try
        {
            var message = new Message
            {
                ChannelId = request.ChannelId,
                SenderId = userId,
                Content = request.Content,
                MessageNonce = request.MessageNonce,
                ContentType = request.ContentType,
                KeyVersion = request.KeyVersion,
                ParentMessageId = request.ParentMessageId
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync(ct);

            // Link attachments to the message
            if (request.Attachments?.Any() == true)
            {
                var attachments = await context.Attachments
                    .Where(a => request.Attachments.Contains(a.Id))
                    .ToListAsync(ct);

                foreach (var attachment in attachments)
                {
                    attachment.MessageId = message.Id;
                }

                await context.SaveChangesAsync(ct);
            }

            await context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync(ct);

            await context.Entry(message)
                .Collection(m => m.Attachments)
                .LoadAsync(ct);

            var messageDto = message.ToDto();

            // Publish through event bus
            await eventBus.PublishAsync(new MessageEvent(request.ChannelId, messageDto), ct);

            return Result<MessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create message");
            return Result<MessageDto>.Failure(Error.Internal("Failed to create message"));
        }
    }

    public async Task<Result<DirectMessageDto>> CreateDirectAsync(
        Guid userId,
        CreateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        // Check rate limit
        if (!await CheckRateLimitAsync(userId, ct))
        {
            return Result<DirectMessageDto>.Failure(Error.TooManyRequests("Message rate limit exceeded"));
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Verify recipient exists and isn't blocked
        var contact = await context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ContactUserId == request.RecipientId, ct);

        if (contact?.Status == ContactStatus.Blocked)
        {
            return Result<DirectMessageDto>.Failure(Error.Forbidden("Cannot message blocked contact"));
        }

        try
        {
            var message = new DirectMessage
            {
                SenderId = userId,
                RecipientId = request.RecipientId,
                Content = request.Content,
                ContentType = request.ContentType,
                MessageNonce = request.MessageNonce,
                KeyVersion = request.KeyVersion,
                ParentMessageId = request.ParentMessageId
            };

            context.DirectMessages.Add(message);
            await context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync(ct);

            await context.Entry(message)
                .Reference(m => m.Recipient)
                .LoadAsync(ct);

            var messageDto = message.ToDto();

            // Publish direct message event
            await eventBus.PublishAsync(new DirectMessageEvent(messageDto), ct);

            return Result<DirectMessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create direct message");
            return Result<DirectMessageDto>.Failure(Error.Internal("Failed to create direct message"));
        }
    }

    public async Task<Result<bool>> DeleteDirectAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.DirectMessages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<bool>.Success(true); // Already deleted
        }

        // Only sender can delete their messages
        if (message.SenderId != userId)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));
        }

        try
        {
            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            // Publish direct message deleted event
            await eventBus.PublishAsync(
                new DirectMessageDeletedEvent(messageId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete direct message {MessageId}", messageId);
            return Result<bool>.Failure(Error.Internal("Failed to delete direct message"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        if (message.SenderId != userId)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));
        }

        try
        {
            // Remove attachments
            if (message.Attachments?.Any() == true)
            {
                context.Attachments.RemoveRange(message.Attachments);
            }

            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            // Publish message deleted event
            await eventBus.PublishAsync(
                new MessageDeletedEvent(message.ChannelId, messageId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return Result<bool>.Failure(Error.Internal("Failed to delete message"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetChannelMessagesAsync(
        Guid channelId,
        int limit,
        DateTime? before = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Start with base query
        var query = context.Messages
            .Where(m => m.ChannelId == channelId);

        // Add before filter if specified
        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        // Apply ordering and includes after all filters
        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Take(limit)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MessageDto>>.Success(
            messages.Select(m => m.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<DirectMessageDto>>> GetDirectMessagesAsync(
        Guid userId,
        Guid otherUserId,
        int limit,
        DateTime? before = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Start with base query
        var query = context.DirectMessages
            .Where(m =>
                m.SenderId == userId && m.RecipientId == otherUserId ||
                m.SenderId == otherUserId && m.RecipientId == userId);

        // Apply before filter first
        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        // Then apply ordering and includes
        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Attachments)
            .Take(limit)
            .ToListAsync(ct);

        return Result<IReadOnlyList<DirectMessageDto>>.Success(
            messages.Select(m => m.ToDto()).ToList());
    }

    public async Task<Result<MessageDto>> UpdateAsync(
        Guid messageId,
        Guid userId,
        UpdateMessageRequest request,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<MessageDto>.Failure(Error.NotFound("Message not found"));
        }

        if (message.SenderId != userId)
        {
            return Result<MessageDto>.Failure(Error.Forbidden("Cannot edit another user's message"));
        }

        if (request.Content.Length > _limitSettings.MaxMessageLength)
        {
            return Result<MessageDto>.Failure(Error.Validation("Message content exceeds maximum length"));
        }

        try
        {
            message.Content = request.Content;
            message.ContentType = request.ContentType;
            message.KeyVersion = request.KeyVersion;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            var messageDto = message.ToDto();

            // Publish message updated event
            await eventBus.PublishAsync(
                new MessageUpdatedEvent(message.ChannelId, messageDto),
                ct);

            return Result<MessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update message {MessageId}", messageId);
            return Result<MessageDto>.Failure(Error.Internal("Failed to update message"));
        }
    }

    public async Task<Result<DirectMessageDto>> UpdateDirectAsync(
        Guid messageId,
        Guid userId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.DirectMessages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<DirectMessageDto>.Failure(Error.NotFound("Message not found"));
        }

        if (message.SenderId != userId)
        {
            return Result<DirectMessageDto>.Failure(Error.Forbidden("Cannot edit another user's message"));
        }

        try
        {
            message.Content = request.Content;
            message.ContentType = request.ContentType;
            message.MessageNonce = request.MessageNonce;
            message.KeyVersion = request.KeyVersion;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            var messageDto = message.ToDto();

            // Publish direct message updated event
            await eventBus.PublishAsync(
                new DirectMessageUpdatedEvent(messageDto),
                ct);

            return Result<DirectMessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update direct message {MessageId}", messageId);
            return Result<DirectMessageDto>.Failure(Error.Internal("Failed to update direct message"));
        }
    }

    public async Task<Result<MessageReactionDto>> AddChannelMessageReactionAsync(
        Guid messageId,
        Guid userId,
        ReactionType type,
        string? customEmoji = null,
        CancellationToken ct = default)
    {
        // Validate custom emoji
        if (type == ReactionType.Custom && string.IsNullOrWhiteSpace(customEmoji))
        {
            return Result<MessageReactionDto>.Failure(
                Error.Validation("Custom emoji is required for custom reactions"));
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Verify message exists and user has access
        var message = await context.Messages
            .Include(m => m.Channel)
            .Include(m => m.Reactions)
            .Include(m => m.Sender)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<MessageReactionDto>.Failure(Error.NotFound("Message not found"));
        }

        if (message.IsDeleted)
        {
            return Result<MessageReactionDto>.Failure(Error.Forbidden("Cannot react to deleted messages"));
        }

        // Verify user is member of the channel
        var isMember = await context.ChannelMembers
            .AnyAsync(m => m.ChannelId == message.ChannelId && m.UserId == userId, ct);

        if (!isMember)
        {
            return Result<MessageReactionDto>.Failure(Error.Forbidden("Cannot react to this message"));
        }

        // Verify not already reacted with same type
        var existingReaction = await context.MessageReactions
            .FirstOrDefaultAsync(r =>
                r.ChannelMessageId == messageId &&
                r.UserId == userId &&
                r.Type == type &&
                r.CustomEmoji == customEmoji, ct);

        if (existingReaction is not null)
        {
            return Result<MessageReactionDto>.Failure(Error.Conflict("Already reacted with this reaction"));
        }

        try
        {
            var user = await context.Users.FindAsync(userId, ct);
            if (user is null)
            {
                return Result<MessageReactionDto>.Failure(Error.NotFound("User not found"));
            }

            var reaction = new MessageReaction
            {
                ChannelMessageId = messageId,
                UserId = userId,
                Type = type,
                CustomEmoji = customEmoji,
                User = user
            };

            context.MessageReactions.Add(reaction);
            await context.SaveChangesAsync(ct);

            var reactionDto = reaction.ToDto();

            // Publish event
            await eventBus.PublishAsync(
                new MessageReactionAddedEvent(message.ChannelId, messageId, reactionDto),
                ct);

            return Result<MessageReactionDto>.Success(reactionDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add reaction to channel message {MessageId}", messageId);
            return Result<MessageReactionDto>.Failure(Error.Internal("Failed to add reaction"));
        }
    }

    public async Task<Result<MessageReactionDto>> AddDirectMessageReactionAsync(
        Guid messageId,
        Guid userId,
        ReactionType type,
        string? customEmoji = null,
        CancellationToken ct = default)
    {
        // Validate custom emoji
        if (type == ReactionType.Custom && string.IsNullOrWhiteSpace(customEmoji))
        {
            return Result<MessageReactionDto>.Failure(
                Error.Validation("Custom emoji is required for custom reactions"));
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Check if it's a direct message
        var directMessage = await context.DirectMessages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Include(m => m.Reactions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (directMessage is null)
        {
            return Result<MessageReactionDto>.Failure(Error.NotFound("Message not found"));
        }

        if (directMessage.IsDeleted)
        {
            return Result<MessageReactionDto>.Failure(Error.Forbidden("Cannot react to deleted messages"));
        }

        // Verify user is sender or recipient
        if (directMessage.SenderId != userId && directMessage.RecipientId != userId)
        {
            return Result<MessageReactionDto>.Failure(Error.Forbidden("Cannot react to this message"));
        }

        // Verify not already reacted with same type
        var existingReaction = await context.MessageReactions
            .FirstOrDefaultAsync(r =>
                r.DirectMessageId == messageId &&
                r.UserId == userId &&
                r.Type == type &&
                r.CustomEmoji == customEmoji, ct);

        if (existingReaction is not null)
        {
            return Result<MessageReactionDto>.Failure(Error.Conflict("Already reacted with this reaction"));
        }

        try
        {
            var user = await context.Users.FindAsync(userId, ct);
            if (user is null)
            {
                return Result<MessageReactionDto>.Failure(Error.NotFound("User not found"));
            }

            var reaction = new MessageReaction
            {
                DirectMessageId = messageId,
                UserId = userId,
                Type = type,
                CustomEmoji = customEmoji,
                User = user
            };

            context.MessageReactions.Add(reaction);
            await context.SaveChangesAsync(ct);

            var reactionDto = reaction.ToDto();

            // Publish event
            await eventBus.PublishAsync(
                new DirectMessageReactionAddedEvent(messageId, reactionDto),
                ct);

            return Result<MessageReactionDto>.Success(reactionDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add reaction to direct message {MessageId}", messageId);
            return Result<MessageReactionDto>.Failure(Error.Internal("Failed to add reaction"));
        }
    }

    public async Task<Result<bool>> RemoveChannelMessageReactionAsync(
        Guid messageId,
        Guid reactionId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var reaction = await context.MessageReactions
            .Include(r => r.Message)
            .FirstOrDefaultAsync(r => r.Id == reactionId && r.ChannelMessageId == messageId, ct);

        if (reaction is null)
        {
            return Result<bool>.Success(true); // Already removed
        }

        if (reaction.Message is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        // Only the user who added the reaction can remove it
        if (reaction.UserId != userId)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot remove another user's reaction"));
        }

        try
        {
            context.MessageReactions.Remove(reaction);
            await context.SaveChangesAsync(ct);

            // Publish event
            await eventBus.PublishAsync(
                new MessageReactionRemovedEvent(reaction.Message.ChannelId, messageId, reactionId, userId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove reaction {ReactionId} from channel message {MessageId}",
                reactionId, messageId);
            return Result<bool>.Failure(Error.Internal("Failed to remove reaction"));
        }
    }

    public async Task<Result<bool>> RemoveDirectMessageReactionAsync(
        Guid messageId,
        Guid reactionId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var reaction = await context.MessageReactions
            .Include(r => r.DirectMessage)
            .FirstOrDefaultAsync(r => r.Id == reactionId && r.DirectMessageId == messageId, ct);

        if (reaction is null)
        {
            return Result<bool>.Success(true); // Already removed
        }

        if (reaction.DirectMessage is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        // Only the user who added the reaction can remove it
        if (reaction.UserId != userId)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot remove another user's reaction"));
        }

        try
        {
            context.MessageReactions.Remove(reaction);
            await context.SaveChangesAsync(ct);

            // Publish event
            await eventBus.PublishAsync(
                new DirectMessageReactionRemovedEvent(messageId, reactionId, userId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove reaction {ReactionId} from direct message {MessageId}",
                reactionId, messageId);
            return Result<bool>.Failure(Error.Internal("Failed to remove reaction"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageReactionDto>>> GetChannelMessageReactionsAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var message = await context.Messages
                .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (message is null)
            {
                return Result<IReadOnlyList<MessageReactionDto>>.Failure(Error.NotFound("Message not found"));
            }

            if (message.IsDeleted)
            {
                return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                    Error.Forbidden("Cannot get reactions for deleted messages"));
            }

            var reactions = message.Reactions
                .OrderBy(r => r.CreatedAt)
                .ToList();

            return Result<IReadOnlyList<MessageReactionDto>>.Success(
                reactions.Select(r => r.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reactions for channel message {MessageId}", messageId);
            return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                Error.Internal("Failed to get reactions"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageReactionDto>>> GetDirectMessageReactionsAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var message = await context.DirectMessages
                .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (message is null)
            {
                return Result<IReadOnlyList<MessageReactionDto>>.Failure(Error.NotFound("Message not found"));
            }

            if (message.IsDeleted)
            {
                return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                    Error.Forbidden("Cannot get reactions for deleted messages"));
            }

            var reactions = message.Reactions
                .OrderBy(r => r.CreatedAt)
                .ToList();

            return Result<IReadOnlyList<MessageReactionDto>>.Success(
                reactions.Select(r => r.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reactions for direct message {MessageId}", messageId);
            return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                Error.Internal("Failed to get reactions"));
        }
    }

    public async Task<Result<bool>> ReplyAsync(
        Guid messageId,
        Guid userId,
        ReplyMessageRequest request,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Channel)
            .Include(m => m.Replies)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        // Verify channel access
        var canAccess = await channelService.CanAccessAsync(userId, message.ChannelId, ct);
        if (canAccess.IsFailure || !canAccess.Value)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
        }

        try
        {
            var reply = new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = message.ChannelId,
                SenderId = userId,
                Content = request.Content,
                Metadata = request.Metadata,
                ReplyToId = messageId,
                SentAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MessageNonce = request.MessageNonce,
                ContentType = ContentType.Text
            };

            context.Messages.Add(reply);
            message.ReplyCount++;

            await context.SaveChangesAsync(ct);

            // Publish events
            await eventBus.PublishAsync(
                new MessageRepliedEvent(messageId, reply.ToDto()),
                ct);

            await eventBus.PublishAsync(
                new ReplyCountUpdatedEvent(messageId, message.ReplyCount),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reply to message {MessageId}", messageId);
            return Result<bool>.Failure(Error.Internal("Failed to reply to message"));
        }
    }

    public async Task<Result<bool>> PinMessageAsync(
        Guid channelId,
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Channel)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChannelId == channelId, ct);

        if (message is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        // Verify channel access and permissions
        var canAccess = await channelService.CanAccessAsync(userId, channelId, ct);
        if (canAccess.IsFailure || !canAccess.Value)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
        }

        var canPin = await channelService.HasPermissionAsync(userId, channelId, PermissionType.PinMessages, ct);
        if (canPin.IsFailure || !canPin.Value)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot pin messages in this channel"));
        }

        try
        {
            message.IsPinned = true;
            message.PinnedAt = DateTime.UtcNow;
            message.PinnedById = userId;

            await context.SaveChangesAsync(ct);

            // Publish event
            await eventBus.PublishAsync(
                new MessagePinnedEvent(channelId, message.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pin message {MessageId} in channel {ChannelId}", messageId, channelId);
            return Result<bool>.Failure(Error.Internal("Failed to pin message"));
        }
    }

    public async Task<Result<bool>> UnpinMessageAsync(
        Guid channelId,
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Channel)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChannelId == channelId, ct);

        if (message is null)
        {
            return Result<bool>.Failure(Error.NotFound("Message not found"));
        }

        if (!message.IsPinned)
        {
            return Result<bool>.Success(true); // Already unpinned
        }

        // Verify channel access and permissions
        var canAccess = await channelService.CanAccessAsync(userId, channelId, ct);
        if (canAccess.IsFailure || !canAccess.Value)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot access channel"));
        }

        var canPin = await channelService.HasPermissionAsync(userId, channelId, PermissionType.PinMessages, ct);
        if (canPin.IsFailure || !canPin.Value)
        {
            return Result<bool>.Failure(Error.Forbidden("Cannot unpin messages in this channel"));
        }

        try
        {
            message.IsPinned = false;
            message.PinnedAt = null;
            message.PinnedById = null;

            await context.SaveChangesAsync(ct);

            // Publish event
            await eventBus.PublishAsync(
                new MessageUnpinnedEvent(channelId, messageId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unpin message {MessageId} in channel {ChannelId}", messageId, channelId);
            return Result<bool>.Failure(Error.Internal("Failed to unpin message"));
        }
    }

    public async Task<Result<MessageDto>> GetChannelMessageAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var message = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (message is null)
            {
                return Result<MessageDto>.Failure(Error.NotFound("Message not found"));
            }

            return Result<MessageDto>.Success(message.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            return Result<MessageDto>.Failure(Error.Internal("Failed to get message"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageReactionDto>>> GetMessageReactionsAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var reactions = await context.MessageReactions
                .Include(r => r.User)
                .Where(r => r.ChannelMessageId == messageId || r.DirectMessageId == messageId)
                .ToListAsync(ct);

            return Result<IReadOnlyList<MessageReactionDto>>.Success(
                reactions.Select(r => r.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reactions for message {MessageId}", messageId);
            return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                Error.Internal("Failed to get message reactions"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageReactionDto>>> GetMessageReactionsByTypeAsync(
        Guid messageId,
        ReactionType type,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var reactions = await context.MessageReactions
                .Include(r => r.User)
                .Where(r => r.ChannelMessageId == messageId || r.DirectMessageId == messageId && r.Type == type)
                .ToListAsync(ct);

            return Result<IReadOnlyList<MessageReactionDto>>.Success(
                reactions.Select(r => r.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reactions of type {Type} for message {MessageId}",
                type, messageId);
            return Result<IReadOnlyList<MessageReactionDto>>.Failure(
                Error.Internal("Failed to get message reactions"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetMessageRepliesAsync(
        Guid messageId,
        int limit,
        DateTime? before,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var query = context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Where(m => m.ParentMessageId == messageId);

            if (before.HasValue)
            {
                query = query.Where(m => m.SentAt < before.Value);
            }

            var replies = await query
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync(ct);

            return Result<IReadOnlyList<MessageDto>>.Success(
                replies.Select(m => m.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get replies for message {MessageId}", messageId);
            return Result<IReadOnlyList<MessageDto>>.Failure(
                Error.Internal("Failed to get message replies"));
        }
    }

    public async Task<Result<int>> GetMessageReplyCountAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var count = await context.Messages
                .CountAsync(m => m.ParentMessageId == messageId, ct);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reply count for message {MessageId}", messageId);
            return Result<int>.Failure(
                Error.Internal("Failed to get message reply count"));
        }
    }

    public async Task<Result<MessageReactionDto?>> GetUserReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var reaction = await context.MessageReactions
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    (r.ChannelMessageId == messageId || r.DirectMessageId == messageId) &&
                    r.UserId == userId, ct);

            return Result<MessageReactionDto?>.Success(reaction?.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user reaction for message {MessageId} and user {UserId}",
                messageId, userId);
            return Result<MessageReactionDto?>.Failure(
                Error.Internal("Failed to get user reaction"));
        }
    }

    public async Task<Result<MessageDto>> GetParentMessageAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var message = await context.Messages
                .Include(m => m.ParentMessage)
                .ThenInclude(p => p!.Sender)
                .Include(m => m.ParentMessage)
                .ThenInclude(p => p!.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (message?.ParentMessage is null)
            {
                return Result<MessageDto>.Failure(Error.NotFound("Parent message not found"));
            }

            return Result<MessageDto>.Success(message.ParentMessage.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get parent message for message {MessageId}", messageId);
            return Result<MessageDto>.Failure(
                Error.Internal("Failed to get parent message"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetUserMentionsAsync(
        Guid userId,
        int limit,
        DateTime? before = null,
        DateTime? after = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        try
        {
            var query = context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Include(m => m.Mentions)
                .Where(m => m.Mentions.Any(u => u.Id == userId));

            if (before.HasValue)
            {
                query = query.Where(m => m.SentAt < before.Value);
            }

            if (after.HasValue)
            {
                query = query.Where(m => m.SentAt > after.Value);
            }

            var messages = await query
                .Take(limit)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync(ct);

            return Result<IReadOnlyList<MessageDto>>.Success(
                messages.Select(m => m.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get mentions for user {UserId}", userId);
            return Result<IReadOnlyList<MessageDto>>.Failure(
                Error.Internal("Failed to get user mentions"));
        }
    }

    private async Task<bool> CheckRateLimitAsync(Guid userId, CancellationToken ct)
    {
        if (_limitSettings?.RateLimits?.Messages == null)
        {
            return true; // No rate limit configured
        }

        var rateLimit = _limitSettings.RateLimits.Messages;
        var now = DateTime.UtcNow;

        var (count, start) = _rateLimits.GetOrAdd(userId, _ => (0, now));

        // Reset rate limit if window expired
        if ((now - start).TotalSeconds >= rateLimit.DurationSeconds)
        {
            _rateLimits.TryUpdate(userId, (1, now), (count, start));
            return true;
        }

        // Increment count if within window
        if (count >= rateLimit.Points)
        {
            return false;
        }

        _rateLimits.TryUpdate(userId, (count + 1, start), (count, start));
        return true;
    }
}
