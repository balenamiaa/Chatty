using System.Net;
using System.Net.Http.Json;

using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Client.Realtime;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Messages;

public sealed class MessageService(
    IHttpClientFactory httpClientFactory,
    IChattyRealtimeClient realtimeClient,
    ILogger<MessageService> logger)
    : BaseService(httpClientFactory, logger, "MessageService"), IMessageService
{
    public async Task<IReadOnlyList<MessageDto>> GetChannelMessagesAsync(
        Guid channelId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Messages.ChannelMessages(channelId, limit, before);
        logger.LogHttpRequest("GET", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get channel messages",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<MessageDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse messages response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<IReadOnlyList<DirectMessageDto>> GetDirectMessagesAsync(
        Guid otherUserId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        var endpoint = ApiEndpoints.Messages.DirectMessages(otherUserId, limit, before);
        logger.LogHttpRequest("GET", endpoint);

        var response = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get direct messages",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<DirectMessageDto>>(ct);
            });

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse messages response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return response;
    }

    public async Task<MessageDto> SendMessageAsync(
        CreateMessageRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Sending message to channel {ChannelId}", request.ChannelId);
        var result = await realtimeClient.SendMessageAsync(request, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task<DirectMessageDto> SendDirectMessageAsync(
        CreateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Sending direct message");
        var result = await realtimeClient.SendDirectMessageAsync(request, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task<MessageDto> UpdateMessageAsync(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Updating message {MessageId}", messageId);
        var result = await realtimeClient.UpdateMessageAsync(messageId, request, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task<DirectMessageDto> UpdateDirectMessageAsync(
        Guid messageId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Updating direct message {MessageId}", messageId);
        var result = await realtimeClient.UpdateDirectMessageAsync(messageId, request, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Deleting message {MessageId}", messageId);
        await realtimeClient.DeleteMessageAsync(messageId, ct);
        logger.LogMethodExit();
    }

    public async Task DeleteDirectMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Deleting direct message {MessageId}", messageId);
        await realtimeClient.DeleteDirectMessageAsync(messageId, ct);
        logger.LogMethodExit();
    }

    public async Task<MessageDto> ReplyToMessageAsync(
        Guid messageId,
        ReplyMessageRequest request,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Replying to message {MessageId}", messageId);
        var result = await realtimeClient.ReplyToMessageAsync(messageId, request, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task<bool> PinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Pinning message {MessageId} in channel {ChannelId}", messageId, channelId);
        var result = await realtimeClient.PinMessageAsync(channelId, messageId, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task<bool> UnpinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Unpinning message {MessageId} in channel {ChannelId}", messageId, channelId);
        var result = await realtimeClient.UnpinMessageAsync(channelId, messageId, ct);
        logger.LogMethodExit();
        return result;
    }

    public async Task AddReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Adding reaction {Reaction} to message {MessageId}", reaction, messageId);
        await realtimeClient.AddReactionAsync(messageId, reaction);
        logger.LogMethodExit();
    }

    public async Task RemoveReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Removing reaction {Reaction} from message {MessageId}", reaction, messageId);
        await realtimeClient.RemoveReactionAsync(messageId, reaction);
        logger.LogMethodExit();
    }

    public async Task<MessageDto> GetMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting message {MessageId}", messageId);

        var endpoint = ApiEndpoints.Messages.Message(messageId);
        logger.LogHttpRequest("GET", endpoint);

        var message = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get message",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<MessageDto>(ct);
            });

        if (message is null)
        {
            throw new ApiException(
                "Failed to parse message response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return message;
    }

    #region Reaction Methods

    public async Task<IReadOnlyList<MessageReactionDto>> GetReactionsAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting reactions for message {MessageId}", messageId);

        var endpoint = ApiEndpoints.Messages.MessageReactions(messageId);
        logger.LogHttpRequest("GET", endpoint);

        var reactions = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get message reactions",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<MessageReactionDto>>(ct);
            });

        if (reactions is null)
        {
            throw new ApiException(
                "Failed to parse reactions response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return reactions;
    }

    public async Task<IReadOnlyList<MessageReactionDto>> GetReactionsByTypeAsync(
        Guid messageId,
        ReactionType type,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting reactions of type {Type} for message {MessageId}", type, messageId);

        var endpoint = ApiEndpoints.Messages.MessageReactionsByType(messageId, type);
        logger.LogHttpRequest("GET", endpoint);

        var reactions = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get message reactions by type",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<MessageReactionDto>>(ct);
            });

        if (reactions is null)
        {
            throw new ApiException(
                "Failed to parse reactions response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return reactions;
    }

    public async Task<MessageReactionDto?> GetUserReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting user {UserId} reaction for message {MessageId}", userId, messageId);

        var endpoint = ApiEndpoints.Messages.UserReaction(messageId, userId);
        logger.LogHttpRequest("GET", endpoint);

        var reaction = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get user reaction",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<MessageReactionDto>(ct);
            });

        logger.LogMethodExit();
        return reaction;
    }

    #endregion

    #region Thread Methods

    public async Task<MessageDto> GetParentMessageAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting parent message for message {MessageId}", messageId);

        var endpoint = ApiEndpoints.Messages.ParentMessage(messageId);
        logger.LogHttpRequest("GET", endpoint);

        var message = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get parent message",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<MessageDto>(ct);
            });

        if (message is null)
        {
            throw new ApiException(
                "Failed to parse message response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return message;
    }

    public async Task<IReadOnlyList<MessageDto>> GetRepliesAsync(
        Guid messageId,
        DateTime? before = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting replies for message {MessageId}", messageId);

        var endpoint = ApiEndpoints.Messages.MessageReplies(messageId, limit, before);
        logger.LogHttpRequest("GET", endpoint);

        var replies = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get message replies",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<MessageDto>>(ct);
            });

        if (replies is null)
        {
            throw new ApiException(
                "Failed to parse replies response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return replies;
    }

    public async Task<int> GetReplyCountAsync(
        Guid messageId,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting reply count for message {MessageId}", messageId);

        var endpoint = ApiEndpoints.Messages.MessageReplyCount(messageId);
        logger.LogHttpRequest("GET", endpoint);

        var count = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get reply count",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<int>(ct);
            });

        logger.LogMethodExit();
        return count;
    }

    public async Task<IReadOnlyList<MessageDto>> GetMentionsAsync(
        Guid userId,
        int limit = 50,
        DateTime? before = null,
        DateTime? after = null,
        CancellationToken ct = default)
    {
        logger.LogMethodEntry();
        logger.LogInformation("Getting mentions for user {UserId}", userId);

        var endpoint = ApiEndpoints.Messages.Mentions(userId, limit, before, after);
        logger.LogHttpRequest("GET", endpoint);

        var mentions = await ExecuteWithPoliciesAsync(
            async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException(
                        "Failed to get user mentions",
                        response.StatusCode);
                }

                return await response.Content.ReadFromJsonAsync<List<MessageDto>>(ct);
            });

        if (mentions is null)
        {
            throw new ApiException(
                "Failed to parse mentions response",
                HttpStatusCode.InternalServerError);
        }

        logger.LogMethodExit();
        return mentions;
    }

    #endregion
}
