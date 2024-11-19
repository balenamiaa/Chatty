using ContentType = Chatty.Shared.Models.Enums.ContentType;

namespace Chatty.Shared.Models.Messages;

public sealed record UpdateMessageRequest(
    byte[] Content,
    ContentType ContentType,
    byte[] MessageNonce,
    int KeyVersion,
    IReadOnlyList<Guid>? Attachments = null);
