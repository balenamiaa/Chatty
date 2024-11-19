using Chatty.Client.Cache;
using Chatty.Client.Models;
using Chatty.Client.State;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Messages;

/// <summary>
///     Cached implementation of message service
/// </summary>
public class CachedMessageService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    IStateManager state,
    IMessageService messageService,
    IUserService userService,
    ILogger<CachedMessageService> logger)
    : BaseService(httpClientFactory, logger, "CachedMessageService"), IMessageService
{
    private readonly IStateManager _state = state;

    public async Task<IReadOnlyList<MessageDto>> GetChannelMessagesAsync(
        Guid channelId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default)
    {
        // Try cache first
        var messages = await cache.GetAsync<List<MessageDto>>(
            CacheKeys.ChannelMessages(channelId),
            ct);

        if (messages is not null)
        {
            // Apply pagination
            var index = before.HasValue
                ? messages.FindIndex(m => m.Id == before.Value)
                : messages.Count;

            if (index < 0)
            {
                index = messages.Count;
            }

            return messages
                .Take(Math.Min(index, limit))
                .ToList();
        }

        // Get from service
        messages = (await messageService.GetChannelMessagesAsync(
            channelId,
            limit,
            before,
            ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.ChannelMessages(channelId),
            messages,
            ct: ct);

        foreach (var message in messages)
        {
            await cache.SetAsync(
                CacheKeys.Message(message.Id),
                message,
                ct: ct);
        }

        return messages;
    }

    public async Task<MessageDto> GetMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        // Try cache first
        var message = await cache.GetAsync<MessageDto>(
            CacheKeys.Message(messageId),
            ct);

        if (message is not null)
        {
            return message;
        }

        // Get from service
        message = await messageService.GetMessageAsync(messageId, ct);

        // Update cache
        await cache.SetAsync(
            CacheKeys.Message(messageId),
            message,
            ct: ct);

        return message;
    }

    public async Task<MessageDto> SendMessageAsync(
        CreateMessageRequest request,
        CancellationToken ct = default)
    {
        // Send through service
        var message = await messageService.SendMessageAsync(request, ct);

        // Update cache
        await cache.SetAsync(
            CacheKeys.Message(message.Id),
            message,
            TimeSpan.FromMinutes(5),
            ct);

        return message;
    }

    public async Task<DirectMessageDto> SendDirectMessageAsync(
        CreateDirectMessageRequest request,
        CancellationToken ct = default) =>
        await messageService.SendDirectMessageAsync(request, ct);

    public async Task<MessageDto> UpdateMessageAsync(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken ct = default)
    {
        // Update through service
        var message = await messageService.UpdateMessageAsync(messageId, request, ct);

        // Update cache
        await cache.SetAsync(
            CacheKeys.Message(message.Id),
            message,
            TimeSpan.FromMinutes(5),
            ct);

        return message;
    }

    public async Task<DirectMessageDto> UpdateDirectMessageAsync(
        Guid messageId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default) =>
        await messageService.UpdateDirectMessageAsync(messageId, request, ct);

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        // Delete through service
        await messageService.DeleteMessageAsync(messageId, ct);

        // Remove from cache
        await cache.RemoveAsync(CacheKeys.Message(messageId), ct);
    }

    public async Task DeleteDirectMessageAsync(Guid messageId, CancellationToken ct = default) =>
        await messageService.DeleteDirectMessageAsync(messageId, ct);

    public async Task<MessageDto> ReplyToMessageAsync(
        Guid messageId,
        ReplyMessageRequest request,
        CancellationToken ct = default)
    {
        var message = await messageService.ReplyToMessageAsync(messageId, request, ct);

        // Cache the new message
        await cache.SetAsync(
            CacheKeys.Message(message.Id),
            message,
            TimeSpan.FromMinutes(5),
            ct);

        // Update reply count in cache
        var replyCountState = await cache.GetAsync<IntState>(
            CacheKeys.MessageReplyCount(messageId),
            ct);

        if (replyCountState is { Value: var replyCount })
        {
            await cache.SetAsync(
                CacheKeys.MessageReplyCount(messageId),
                new IntState(replyCount + 1),
                TimeSpan.FromMinutes(5),
                ct);
        }

        return message;
    }

    public async Task<bool> PinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default) =>
        await messageService.PinMessageAsync(channelId, messageId, ct);

    public async Task<bool> UnpinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default) =>
        await messageService.UnpinMessageAsync(channelId, messageId, ct);

    public async Task AddReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default) =>
        await messageService.AddReactionAsync(messageId, reaction, ct);

    public async Task RemoveReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default) =>
        await messageService.RemoveReactionAsync(messageId, reaction, ct);

    public async Task<IReadOnlyList<DirectMessageDto>> GetDirectMessagesAsync(
        Guid otherUserId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default)
    {
        var currentUser = await GetCurrentUserAsync(ct);

        // Try cache first
        var messages = await cache.GetAsync<List<DirectMessageDto>>(
            CacheKeys.DirectMessages(currentUser.Id, otherUserId),
            ct);

        if (messages is not null)
        {
            // Apply pagination
            var index = before.HasValue
                ? messages.FindIndex(m => m.Id == before.Value)
                : messages.Count;

            if (index < 0)
            {
                index = messages.Count;
            }

            return messages
                .Take(Math.Min(index, limit))
                .ToList();
        }

        // Get from service
        messages = (await messageService.GetDirectMessagesAsync(
            otherUserId,
            limit,
            before,
            ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.DirectMessages(currentUser.Id, otherUserId),
            messages,
            TimeSpan.FromMinutes(5),
            ct);

        foreach (var message in messages)
        {
            await cache.SetAsync(
                CacheKeys.DirectMessage(message.Id),
                message,
                TimeSpan.FromMinutes(5),
                ct);
        }

        return messages ?? [];
    }

    public async Task<IReadOnlyList<MessageReactionDto>> GetReactionsAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        // Try cache first
        var reactions = await cache.GetAsync<List<MessageReactionDto>>(
            CacheKeys.MessageReactions(messageId),
            ct);

        if (reactions is not null)
        {
            return reactions;
        }

        // Get from service
        reactions = (await messageService.GetReactionsAsync(messageId, ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.MessageReactions(messageId),
            reactions,
            TimeSpan.FromMinutes(5),
            ct);

        return reactions;
    }

    public async Task<IReadOnlyList<MessageReactionDto>> GetReactionsByTypeAsync(
        Guid messageId,
        ReactionType type,
        CancellationToken ct = default)
    {
        // Try cache first
        var reactions = await cache.GetAsync<List<MessageReactionDto>>(
            CacheKeys.MessageReactionsByType(messageId, type.ToString()),
            ct);

        if (reactions is not null)
        {
            return reactions;
        }

        // Get from service
        reactions = (await messageService.GetReactionsByTypeAsync(messageId, type, ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.MessageReactionsByType(messageId, type.ToString()),
            reactions,
            TimeSpan.FromMinutes(5),
            ct);

        return reactions;
    }

    public async Task<MessageReactionDto?> GetUserReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        // Try cache first
        var reactions = await cache.GetAsync<List<MessageReactionDto>>(
            CacheKeys.UserReactions(userId),
            ct);

        if (reactions is not null)
        {
            var reaction = reactions.FirstOrDefault(r => r.MessageId == messageId);
            if (reaction is not null)
            {
                return reaction;
            }
        }

        // Get from service
        var userReaction = await messageService.GetUserReactionAsync(messageId, userId, ct);

        if (userReaction is not null)
        {
            // Update user reactions cache
            reactions ??= [];
            reactions.Add(userReaction);

            await cache.SetAsync(
                CacheKeys.UserReactions(userId),
                reactions,
                TimeSpan.FromMinutes(5),
                ct);
        }

        return userReaction;
    }

    public async Task<MessageDto?> GetParentMessageAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        // Try cache first
        var message = await cache.GetAsync<MessageDto>(
            CacheKeys.MessageParent(messageId),
            ct);

        if (message is not null)
        {
            return message;
        }

        // Get from service
        message = await messageService.GetParentMessageAsync(messageId, ct);

        if (message is not null)
        {
            // Update cache
            await cache.SetAsync(
                CacheKeys.MessageParent(messageId),
                message,
                TimeSpan.FromMinutes(5),
                ct);

            await cache.SetAsync(
                CacheKeys.Message(message.Id),
                message,
                TimeSpan.FromMinutes(5),
                ct);
        }

        return message;
    }

    public async Task<IReadOnlyList<MessageDto>> GetRepliesAsync(
        Guid messageId,
        DateTime? before = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        // Try cache first
        var replies = await cache.GetAsync<List<MessageDto>>(
            CacheKeys.MessageReplies(messageId),
            ct);

        if (replies is not null)
        {
            // Apply pagination
            if (before.HasValue)
            {
                replies = replies
                    .Where(r => r.SentAt < before.Value)
                    .Take(limit)
                    .ToList();
            }
            else
            {
                replies = replies
                    .Take(limit)
                    .ToList();
            }

            return replies;
        }

        // Get from service
        replies = (await messageService.GetRepliesAsync(messageId, before, limit, ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.MessageReplies(messageId),
            replies,
            TimeSpan.FromMinutes(5),
            ct);

        foreach (var reply in replies)
        {
            await cache.SetAsync(
                CacheKeys.Message(reply.Id),
                reply,
                TimeSpan.FromMinutes(5),
                ct);
        }

        return replies;
    }

    public async Task<int> GetReplyCountAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        // Try cache first
        var replyCount = await cache.GetAsync<IntState>(
            CacheKeys.MessageReplyCount(messageId),
            ct);

        if (replyCount is { Value: var count })
        {
            return count;
        }

        // Get from service
        var result = await messageService.GetReplyCountAsync(messageId, ct);

        // Update cache
        await cache.SetAsync(
            CacheKeys.MessageReplyCount(messageId),
            new IntState(result),
            TimeSpan.FromMinutes(5),
            ct);

        return result;
    }

    public async Task<IReadOnlyList<MessageDto>> GetMentionsAsync(
        Guid userId,
        int limit = 50,
        DateTime? before = null,
        DateTime? after = null,
        CancellationToken ct = default)
    {
        // Try cache first
        var mentions = await cache.GetAsync<List<MessageDto>>(
            CacheKeys.UserMentions(userId),
            ct);

        if (mentions is not null)
        {
            // Apply pagination and filtering
            var query = mentions.AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(m => m.SentAt < before.Value);
            }

            if (after.HasValue)
            {
                query = query.Where(m => m.SentAt > after.Value);
            }

            mentions = query
                .Take(limit)
                .ToList();

            return mentions;
        }

        // Get from service
        mentions = (await messageService.GetMentionsAsync(userId, limit, before, after, ct)).ToList();

        // Update cache
        await cache.SetAsync(
            CacheKeys.UserMentions(userId),
            mentions,
            TimeSpan.FromMinutes(5),
            ct);

        foreach (var mention in mentions)
        {
            await cache.SetAsync(
                CacheKeys.Message(mention.Id),
                mention,
                TimeSpan.FromMinutes(5),
                ct);
        }

        return mentions;
    }

    private async Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var user = await userService.GetCurrentUserAsync(ct);
        if (user is null)
        {
            throw new InvalidOperationException("User not logged in");
        }

        return user;
    }
}
