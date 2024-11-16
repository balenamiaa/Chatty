using System.Net.Mime;

using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

using ContentType = Chatty.Shared.Models.Enums.ContentType;

namespace Chatty.Shared.Models.Messages;

public record MessageDto(
    Guid Id,
    Guid ChannelId,
    UserDto Sender,
    byte[] Content,
    ContentType ContentType,
    DateTime SentAt,
    DateTime UpdatedAt,
    bool IsDeleted,
    byte[] MessageNonce,
    int KeyVersion,
    Guid? ParentMessageId,
    int ReplyCount,
    IReadOnlyList<AttachmentDto> Attachments,
    IReadOnlyList<MessageReactionDto> Reactions);
