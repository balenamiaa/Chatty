using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Events;

public sealed record TypingEvent(
    Guid ChannelId,
    UserDto User,
    bool IsTyping);

public sealed record DirectTypingEvent(
    Guid UserId,
    UserDto User,
    bool IsTyping);
