using Chatty.Shared.Models.Messages;

namespace Chatty.Shared.Realtime.Events;

/// <summary>
///     Event published when a reaction is added to a channel message
/// </summary>
/// <param name="ChannelId">The channel ID</param>
/// <param name="MessageId">The message ID</param>
/// <param name="Reaction">The reaction that was added</param>
public record MessageReactionAddedEvent(
    Guid ChannelId,
    Guid MessageId,
    MessageReactionDto Reaction);

/// <summary>
///     Event published when a reaction is removed from a channel message
/// </summary>
/// <param name="ChannelId">The channel ID</param>
/// <param name="MessageId">The message ID</param>
/// <param name="ReactionId">The ID of the reaction that was removed</param>
/// <param name="UserId">The ID of the user who removed the reaction</param>
public record MessageReactionRemovedEvent(
    Guid ChannelId,
    Guid MessageId,
    Guid ReactionId,
    Guid UserId);

/// <summary>
///     Event published when a reaction is added to a direct message
/// </summary>
/// <param name="MessageId">The message ID</param>
/// <param name="Reaction">The reaction that was added</param>
public record DirectMessageReactionAddedEvent(
    Guid MessageId,
    MessageReactionDto Reaction);

/// <summary>
///     Event published when a reaction is removed from a direct message
/// </summary>
/// <param name="MessageId">The message ID</param>
/// <param name="ReactionId">The ID of the reaction that was removed</param>
/// <param name="UserId">The ID of the user who removed the reaction</param>
public record DirectMessageReactionRemovedEvent(
    Guid MessageId,
    Guid ReactionId,
    Guid UserId);
