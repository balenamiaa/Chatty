using ContentType = Chatty.Shared.Models.Enums.ContentType;

namespace Chatty.Shared.Models.Messages;

public sealed record CreateMessageRequest(
    Guid ChannelId,
    byte[] Content,
    ContentType ContentType,
    byte[] MessageNonce,
    int KeyVersion,
    Guid? ParentMessageId = null,
    IReadOnlyList<Guid>? Attachments = null);
