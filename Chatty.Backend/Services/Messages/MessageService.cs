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

namespace Chatty.Backend.Services.Messages;

public sealed class MessageService : IMessageService
{
    private readonly ChattyDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ICryptoProvider _crypto;
    private readonly ILogger<MessageService> _logger;
    private readonly LimitSettings _limitSettings;

    public MessageService(
        ChattyDbContext context,
        IEventBus eventBus,
        ICryptoProvider crypto,
        ILogger<MessageService> logger,
        IOptions<LimitSettings> limitSettings)
    {
        _context = context;
        _eventBus = eventBus;
        _crypto = crypto;
        _logger = logger;
        _limitSettings = limitSettings.Value;
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

        // Verify channel exists and user has access
        var channel = await _context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
            return Result<MessageDto>.Failure(Error.NotFound("Channel not found"));

        if (!channel.Members.Any(m => m.UserId == userId))
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

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(ct);

            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync(ct);

            var messageDto = message.ToDto();

            // Only publish through event bus - remove direct hub broadcast
            await _eventBus.PublishAsync(new MessageEvent(request.ChannelId, messageDto), ct);

            return Result<MessageDto>.Success(messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message");
            return Result<MessageDto>.Failure(Error.Internal("Failed to create message"));
        }
    }

    private async Task<bool> CheckRateLimitAsync(Guid userId, CancellationToken ct)
    {
        var rateLimit = _limitSettings.RateLimits.Messages;
        var cutoff = DateTime.UtcNow.AddSeconds(-rateLimit.DurationSeconds);

        var messageCount = await _context.Messages
            .CountAsync(m =>
                    m.SenderId == userId &&
                    m.SentAt > cutoff,
                ct);

        return messageCount < rateLimit.Points;
    }

    public async Task<Result<DirectMessageDto>> CreateDirectAsync(
        Guid userId,
        CreateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        // Verify recipient exists
        var recipient = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.RecipientId, ct);

        if (recipient is null)
            return Result<DirectMessageDto>.Failure(Error.NotFound("Recipient not found"));

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

            _context.DirectMessages.Add(message);
            await _context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync(ct);

            await _context.Entry(message)
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

    public async Task<Result<bool>> DeleteAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<bool>.Failure(Error.NotFound("Message not found"));

        if (message.SenderId != userId)
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));

        try
        {
            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

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

    public async Task<Result<bool>> DeleteDirectAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        var message = await _context.DirectMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        if (message is null)
            return Result<bool>.Failure(Error.NotFound("Message not found"));

        if (message.SenderId != userId)
            return Result<bool>.Failure(Error.Forbidden("Cannot delete another user's message"));

        try
        {
            message.IsDeleted = true;
            await _context.SaveChangesAsync(ct);

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

    public async Task<Result<IReadOnlyList<MessageDto>>> GetChannelMessagesAsync(
        Guid channelId,
        int limit,
        DateTime? before = null,
        CancellationToken ct = default)
    {
        // Start with base query
        var query = _context.Messages
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
        var query = _context.DirectMessages
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
}