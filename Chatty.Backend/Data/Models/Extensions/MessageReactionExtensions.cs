using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Data.Models.Extensions;

public static class MessageReactionExtensions
{
    public static MessageReactionDto ToDto(this MessageReaction reaction) => new(
        Id: reaction.Id,
        MessageId: reaction.ChannelMessageId ?? reaction.DirectMessageId ?? Guid.Empty,
        User: reaction.User!.ToDto(),
        Type: reaction.Type,
        CustomEmoji: reaction.CustomEmoji,
        CreatedAt: reaction.CreatedAt);
}
