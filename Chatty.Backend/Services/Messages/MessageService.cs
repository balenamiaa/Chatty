using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Shared.Crypto;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Realtime.Events;
using Chatty.Shared.Realtime.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Services.Messages;

public sealed class MessageService(
    IDbContextFactory<ChattyDbContext> contextFactory,
    IEventBus eventBus,
    ICryptoProvider crypto,
    ILogger<MessageService> logger,
    IOptions<LimitSettings> limitSettings) : IMessageService
{
    private readonly IDbContextFactory<ChattyDbContext> _contextFactory = contextFactory;
    private readonly IEventBus _eventBus = eventBus;
    private readonly ICryptoProvider _crypto = crypto;
    private readonly ILogger<MessageService> _logger = logger;
    private readonly LimitSettings _limitSettings = limitSettings.Value;
    private static readonly ConcurrentDictionary<Guid, (int Count, DateTime Start)> _rateLimits = new();

    private async Task<bool> CheckRateLimitAsync(Guid userId, CancellationToken ct)
    {
        if (_limitSettings?.RateLimits?.Messages == null)
            return true;  // No rate limit configured

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
            return false;

        _rateLimits.TryUpdate(userId, (count + 1, start), (count, start));
        return true;
    }

    public async Task<Result<MessageDto>> CreateAsync(
        Guid userId,
        CreateMessageRequest request,
        CancellationToken ct = default)
    {
        // Validate message length
        if (request.Content.Length > _limitSettings.MaxMessageLength)
            return Result<MessageDto>.Failure(
                Error.Validation($"Message exceeds maximum length of {_limitSettings.MaxMessageLength} bytes"));

        // Check rate limit
        if (!await CheckRateLimitAsync(userId, ct))
            return Result<MessageDto>.Failure(Error.TooManyRequests("Message rate limit exceeded"));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Verify channel exists and user has access
        var channel = await context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
            return Result<MessageDto>.Failure(Error.NotFound("Channel not found"));

        if (channel.Members.All(m => m.UserId != userId))
            return Result<MessageDto>.Failure(Error.Forbidden("User is not a member of this channel"));

        try
        {
            var message = new Message
            {
                ChannelId = request.ChannelId,
                SenderId = userId,
                Content = request.Content,
                ContentType = request.ContentType,
                MessageNonce = request.MessageNonce,
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
            await _eventBus.PublishAsync(new MessageEvent(request.ChannelId, messageDto), ct);

            return Result<MessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message");
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
            return Result<DirectMessageDto>.Failure(Error.TooManyRequests("Message rate limit exceeded"));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Verify recipient exists and isn't blocked
        var contact = await context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ContactUserId == request.RecipientId, ct);

        if (contact?.Status == ContactStatus.Blocked)
            return Result<DirectMessageDto>.Failure(Error.Forbidden("Cannot message blocked contact"));

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
            await _eventBus.PublishAsync(new DirectMessageEvent(messageDto), ct);

            return Result<DirectMessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create direct message");
            return Result<DirectMessageDto>.Failure(Error.Internal("Failed to create direct message"));
        }
    }

    public async Task<Result<bool>> DeleteDirectAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var message = await context.DirectMessages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<bool>.Success(true); // Already deleted

        // Only sender can delete their messages
        if (message.SenderId != userId)
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));

        try
        {
            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            // Publish direct message deleted event
            await _eventBus.PublishAsync(
                new DirectMessageDeletedEvent(messageId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete direct message {MessageId}", messageId);
            return Result<bool>.Failure(Error.Internal("Failed to delete direct message"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<bool>.Failure(Error.NotFound("Message not found"));

        if (message.SenderId != userId)
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));

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
            await _eventBus.PublishAsync(
                new MessageDeletedEvent(message.ChannelId, messageId),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            return Result<bool>.Failure(Error.Internal("Failed to delete message"));
        }
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetChannelMessagesAsync(
        Guid channelId,
        int limit,
        DateTime? before = null,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

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
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Start with base query
        var query = context.DirectMessages
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == otherUserId) ||
                (m.SenderId == otherUserId && m.RecipientId == userId));

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
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var message = await context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<MessageDto>.Failure(Error.NotFound("Message not found"));

        if (message.SenderId != userId)
            return Result<MessageDto>.Failure(Error.Forbidden("Cannot edit another user's message"));

        try
        {
            message.Content = request.Content;
            message.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            var messageDto = message.ToDto();

            // Publish message updated event
            await _eventBus.PublishAsync(
                new MessageUpdatedEvent(message.ChannelId, messageDto),
                ct);

            return Result<MessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message {MessageId}", messageId);
            return Result<MessageDto>.Failure(Error.Internal("Failed to update message"));
        }
    }

    public async Task<Result<DirectMessageDto>> UpdateDirectAsync(
        Guid messageId,
        Guid userId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var message = await context.DirectMessages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<DirectMessageDto>.Failure(Error.NotFound("Message not found"));

        if (message.SenderId != userId)
            return Result<DirectMessageDto>.Failure(Error.Forbidden("Cannot edit another user's message"));

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
            await _eventBus.PublishAsync(
                new DirectMessageUpdatedEvent(messageDto),
                ct);

            return Result<DirectMessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update direct message {MessageId}", messageId);
            return Result<DirectMessageDto>.Failure(Error.Internal("Failed to update direct message"));
        }
    }
}