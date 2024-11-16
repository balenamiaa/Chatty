using System.Net.Mime;

using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

using ContentType = Chatty.Shared.Models.Enums.ContentType;

namespace Chatty.Shared.Models.Messages;

public record DirectMessageDto(
    Guid Id,
    UserDto Sender,
    UserDto Recipient,
    byte[] Content,
    ContentType ContentType,
    DateTime SentAt,
    bool IsDeleted,
    byte[] MessageNonce,
    int KeyVersion,
    Guid? ParentMessageId,
    int ReplyCount,
    IReadOnlyList<AttachmentDto> Attachments,
    IReadOnlyList<MessageReactionDto> Reactions);
