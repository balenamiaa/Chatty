using Chatty.Shared.Models.Messages;

namespace Chatty.Shared.Realtime.Events;

public sealed record MessageEvent(
    Guid ChannelId,
    MessageDto Message);

public sealed record DirectMessageEvent(
    DirectMessageDto Message);

public sealed record MessageDeletedEvent(
    Guid ChannelId,
    Guid MessageId);

public sealed record DirectMessageDeletedEvent(
    Guid MessageId);
