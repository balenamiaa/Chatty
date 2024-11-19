using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing messages, direct messages, reactions, and threads
/// </summary>
public interface IMessageService
{
    #region Message Operations

    /// <summary>
    ///     Gets messages from a channel
    /// </summary>
    Task<IReadOnlyList<MessageDto>> GetChannelMessagesAsync(
        Guid channelId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Gets a specific message by ID
    /// </summary>
    Task<MessageDto> GetMessageAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>
    ///     Sends a new message to a channel
    /// </summary>
    Task<MessageDto> SendMessageAsync(
        CreateMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing message
    /// </summary>
    Task<MessageDto> UpdateMessageAsync(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a message
    /// </summary>
    Task DeleteMessageAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>
    ///     Pins a message in a channel
    /// </summary>
    Task<bool> PinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    ///     Unpins a message from a channel
    /// </summary>
    Task<bool> UnpinMessageAsync(
        Guid channelId,
        Guid messageId,
        CancellationToken ct = default);

    #endregion

    #region Direct Message Operations

    /// <summary>
    ///     Gets direct messages between the current user and another user
    /// </summary>
    Task<IReadOnlyList<DirectMessageDto>> GetDirectMessagesAsync(
        Guid userId,
        int limit = 50,
        Guid? before = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Sends a direct message to another user
    /// </summary>
    Task<DirectMessageDto> SendDirectMessageAsync(
        CreateDirectMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing direct message
    /// </summary>
    Task<DirectMessageDto> UpdateDirectMessageAsync(
        Guid messageId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a direct message
    /// </summary>
    Task DeleteDirectMessageAsync(Guid messageId, CancellationToken ct = default);

    #endregion

    #region Reaction Operations

    /// <summary>
    ///     Add a reaction to a message
    /// </summary>
    Task AddReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default);

    /// <summary>
    ///     Remove a reaction from a message
    /// </summary>
    Task RemoveReactionAsync(
        Guid messageId,
        string reaction,
        CancellationToken ct = default);

    /// <summary>
    ///     Get all reactions for a message
    /// </summary>
    Task<IReadOnlyList<MessageReactionDto>> GetReactionsAsync(
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    ///     Get reactions by type for a message
    /// </summary>
    Task<IReadOnlyList<MessageReactionDto>> GetReactionsByTypeAsync(
        Guid messageId,
        ReactionType type,
        CancellationToken ct = default);

    /// <summary>
    ///     Get user's reaction to a message
    /// </summary>
    Task<MessageReactionDto?> GetUserReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default);

    #endregion

    #region Thread Operations

    /// <summary>
    ///     Reply to a message
    /// </summary>
    Task<MessageDto> ReplyToMessageAsync(
        Guid messageId,
        ReplyMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Get the parent message for a thread
    /// </summary>
    Task<MessageDto?> GetParentMessageAsync(
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    ///     Get replies to a message
    /// </summary>
    Task<IReadOnlyList<MessageDto>> GetRepliesAsync(
        Guid messageId,
        DateTime? before = null,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    ///     Get the reply count for a message
    /// </summary>
    Task<int> GetReplyCountAsync(
        Guid messageId,
        CancellationToken ct = default);

    /// <summary>
    ///     Get messages that mention the specified user
    /// </summary>
    Task<IReadOnlyList<MessageDto>> GetMentionsAsync(
        Guid userId,
        int limit = 50,
        DateTime? before = null,
        DateTime? after = null,
        CancellationToken ct = default);

    #endregion
}
