using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Data.Models.Extensions;

public static class MessageReactionExtensions
{
    public static MessageReactionDto ToDto(this MessageReaction reaction) => new(
        reaction.Id,
        reaction.ChannelMessageId ?? reaction.DirectMessageId ?? Guid.Empty,
        reaction.User!.ToDto(),
        reaction.Type,
        reaction.CustomEmoji,
        reaction.CreatedAt);
}
