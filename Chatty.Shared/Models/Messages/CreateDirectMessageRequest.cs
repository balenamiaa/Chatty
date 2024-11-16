using System.Collections.Generic;

using ContentType = Chatty.Shared.Models.Enums.ContentType;

namespace Chatty.Shared.Models.Messages;

public sealed record CreateDirectMessageRequest(
    Guid RecipientId,
    byte[] Content,
    ContentType ContentType,
    byte[] MessageNonce,
    int KeyVersion,
    Guid? ParentMessageId = null,
    IReadOnlyList<Guid>? Attachments = null);
