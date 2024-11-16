using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Messages;

public record MessageReactionDto(
    Guid Id,
    Guid MessageId,
    UserDto User,
    ReactionType Type,
    string? CustomEmoji,
    DateTime CreatedAt);
